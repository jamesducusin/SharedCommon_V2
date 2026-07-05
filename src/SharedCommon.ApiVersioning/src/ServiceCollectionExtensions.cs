using Asp.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SharedCommon.ApiVersioning;

/// <summary>DI registration for SharedCommon.ApiVersioning.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers API versioning with the strategy and defaults defined in
    /// <c>SharedCommon:ApiVersioning</c> configuration.
    ///
    /// <code>
    /// // Program.cs
    /// builder.Services.AddSharedApiVersioning(builder.Configuration);
    /// </code>
    ///
    /// Then decorate controllers with <c>[ApiVersion("1.0")]</c>.
    /// </summary>
    public static IServiceCollection AddSharedApiVersioning(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new ApiVersioningOptions();
        configuration.GetSection(ApiVersioningOptions.SectionName).Bind(options);

        services
            .AddOptions<ApiVersioningOptions>()
            .Bind(configuration.GetSection(ApiVersioningOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var parsed = System.Version.TryParse(options.DefaultVersion, out var version)
            ? new ApiVersion(version.Major, version.Minor)
            : new ApiVersion(1, 0);

        services.AddApiVersioning(o =>
        {
            o.DefaultApiVersion = parsed;
            o.AssumeDefaultVersionWhenUnspecified = options.AssumeDefaultWhenUnspecified;
            o.ReportApiVersions = options.ReportApiVersions;

            var readers = new List<IApiVersionReader>();

            if (options.Strategy.UrlSegment)
                readers.Add(new UrlSegmentApiVersionReader());

            if (options.Strategy.QueryString)
                readers.Add(new QueryStringApiVersionReader(options.Strategy.QueryStringParameterName));

            if (options.Strategy.Header)
                readers.Add(new HeaderApiVersionReader(options.Strategy.HeaderName));

            if (options.Strategy.MediaType)
                readers.Add(new MediaTypeApiVersionReader());

            if (readers.Count > 0)
                o.ApiVersionReader = readers.Count == 1
                    ? readers[0]
                    : ApiVersionReader.Combine([.. readers]);
        })
        .AddApiExplorer(o =>
        {
            o.GroupNameFormat = "'v'VVV";
            o.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}
