using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;
using Serilog.Sinks.Elasticsearch;

namespace SharedCommon.Logging;

/// <summary>DI registration extensions for SharedCommon.Logging.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SharedCommon structured logging (Serilog) to the DI container.
    ///
    /// Automatically configures:
    /// <list type="bullet">
    ///   <item>Console sink (enabled by default).</item>
    ///   <item>File rolling sink (opt-in).</item>
    ///   <item>Elasticsearch sink (opt-in).</item>
    ///   <item><see cref="CorrelationIdEnricher"/> — registered for consumers.</item>
    ///   <item>Machine name, environment, thread, and application name enrichers.</item>
    ///   <item><see cref="LoggingHealthCheck"/> registered under the key <c>logging</c>.</item>
    /// </list>
    ///
    /// Correlation IDs are propagated via <c>Serilog.Context.LogContext</c>; push properties
    /// using <see cref="LogContext.Property"/> or <c>ILogger.BeginScope</c> in middleware.
    ///
    /// Example:
    /// <code>
    /// builder.Services.AddSharedCommonLogging(builder.Configuration);
    /// </code>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either argument is null.</exception>
    public static IServiceCollection AddSharedCommonLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<LoggingOptions>()
            .BindConfiguration(LoggingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpContextAccessor();

        // Read options synchronously for Serilog pipeline construction.
        var opts = configuration.GetSection(LoggingOptions.SectionName).Get<LoggingOptions>()
                   ?? new LoggingOptions();

        var serilogLogger = BuildSerilogLogger(opts);

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(serilogLogger, dispose: true);
        });

        // Register the enricher as a service for consumers who want to use it directly.
        services.AddSingleton<CorrelationIdEnricher>(sp =>
        {
            var accessor = sp.GetRequiredService<IHttpContextAccessor>();
            var loggingOpts = sp.GetRequiredService<IOptions<LoggingOptions>>().Value;
            return new CorrelationIdEnricher(accessor, loggingOpts.CorrelationId);
        });

        services.AddHealthChecks()
            .AddCheck<LoggingHealthCheck>("logging");

        return services;
    }

    private static Serilog.Core.Logger BuildSerilogLogger(LoggingOptions opts)
    {
        var minLevel = ParseLogLevel(opts.MinimumLevel);

        var config = new LoggerConfiguration()
            .MinimumLevel.Is(minLevel)
            .Enrich.FromLogContext()  // Picks up BeginScope / LogContext.PushProperty
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("ApplicationName", opts.ApplicationName);

        if (opts.Serilog.Destructure.Enabled)
        {
            config = config
                .Destructure.ToMaximumStringLength(opts.Serilog.Destructure.MaxStringLength)
                .Destructure.ToMaximumDepth(opts.Serilog.Destructure.MaxDepth)
                .Destructure.ToMaximumCollectionCount(opts.Serilog.Destructure.MaxCollectionCount);
        }

        foreach (var pattern in opts.ExcludePatterns)
        {
            var captured = pattern;
            config = config.Filter.ByExcluding(
                Serilog.Filters.Matching.WithProperty<string>(
                    "MessageTemplate",
                    v => v.Contains(captured, StringComparison.OrdinalIgnoreCase)));
        }

        if (opts.Console.Enabled)
            AddConsoleSink(config, opts.Console, opts.Serilog.Format);

        if (opts.File.Enabled)
            AddFileSink(config, opts.File, opts.Serilog.Format);

        if (opts.Elasticsearch.Enabled && !string.IsNullOrWhiteSpace(opts.Elasticsearch.Url))
        {
            try
            {
                AddElasticsearchSink(config, opts.Elasticsearch, opts.ApplicationName);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine(
                    $"[SharedCommon.Logging] Invalid Elasticsearch URL '{opts.Elasticsearch.Url}': {ex.Message}. Sink skipped.");
            }
        }

        return config.CreateLogger();
    }

    private static void AddConsoleSink(LoggerConfiguration config, ConsoleSinkOptions opts, string format)
    {
        switch (format.ToUpperInvariant())
        {
            case "COMPACTJSON":
                config.WriteTo.Console(new CompactJsonFormatter());
                break;
            case "TEXT":
                config.WriteTo.Console(
                    outputTemplate: opts.IncludeTimestamp
                        ? "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}"
                        : "[{Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}");
                break;
            default: // Json
                config.WriteTo.Console(new JsonFormatter());
                break;
        }
    }

    private static void AddFileSink(LoggerConfiguration config, FileSinkOptions opts, string format)
    {
        var interval = Enum.TryParse<Serilog.RollingInterval>(opts.RollingInterval, ignoreCase: true, out var parsed)
            ? parsed
            : Serilog.RollingInterval.Day;

        switch (format.ToUpperInvariant())
        {
            case "COMPACTJSON":
                config.WriteTo.File(new CompactJsonFormatter(), opts.Path,
                    rollingInterval: interval,
                    retainedFileCountLimit: opts.RetainedFileCountLimit,
                    fileSizeLimitBytes: opts.FileSizeLimit);
                break;
            case "TEXT":
                config.WriteTo.File(opts.Path,
                    rollingInterval: interval,
                    retainedFileCountLimit: opts.RetainedFileCountLimit,
                    fileSizeLimitBytes: opts.FileSizeLimit,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}");
                break;
            default: // Json
                config.WriteTo.File(new JsonFormatter(), opts.Path,
                    rollingInterval: interval,
                    retainedFileCountLimit: opts.RetainedFileCountLimit,
                    fileSizeLimitBytes: opts.FileSizeLimit);
                break;
        }
    }

    private static void AddElasticsearchSink(
        LoggerConfiguration config,
        ElasticsearchSinkOptions opts,
        string applicationName)
    {
        var esOptions = new Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions(new Uri(opts.Url))
        {
            IndexFormat = opts.IndexFormat,
            BatchPostingLimit = opts.BatchSize,
            Period = TimeSpan.FromMilliseconds(opts.Period),
            AutoRegisterTemplate = true,
            AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
            FailureCallback = logEvent =>
                System.Console.Error.WriteLine(
                    $"[SharedCommon.Logging] Elasticsearch sink failed to ship event at {logEvent.Timestamp:O}"),
            EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog
        };

        if (!string.IsNullOrWhiteSpace(opts.Username) && !string.IsNullOrWhiteSpace(opts.Password))
        {
            var username = opts.Username;
            var password = opts.Password;
            esOptions.ModifyConnectionSettings = conn => conn.BasicAuthentication(username, password);
        }

        config.WriteTo.Elasticsearch(esOptions);
    }

    private static LogEventLevel ParseLogLevel(string level) => level.ToUpperInvariant() switch
    {
        "DEBUG" => LogEventLevel.Debug,
        "WARNING" => LogEventLevel.Warning,
        "ERROR" => LogEventLevel.Error,
        "CRITICAL" or "FATAL" => LogEventLevel.Fatal,
        _ => LogEventLevel.Information
    };
}
