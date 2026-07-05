using Grpc.HealthCheck;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SharedCommon.Grpc;

/// <summary>DI registration extensions for SharedCommon gRPC infrastructure.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers gRPC with SharedCommon interceptors (exception mapping, correlation ID,
    /// structured logging) and configures health checks and reflection per options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    public static IServiceCollection AddSharedGrpc(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<GrpcOptions>()
            .BindConfiguration(GrpcOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Read eagerly to configure gRPC options at registration time
        var grpcOptions = configuration
            .GetSection(GrpcOptions.SectionName)
            .Get<GrpcOptions>() ?? new GrpcOptions();

        services.AddGrpc(options =>
        {
            // Order: Exception (outermost) → CorrelationId → Logging (innermost)
            options.Interceptors.Add<ExceptionInterceptor>();
            options.Interceptors.Add<CorrelationIdInterceptor>();
            options.Interceptors.Add<LoggingInterceptor>();
            options.MaxReceiveMessageSize = grpcOptions.MaxReceiveMessageSizeBytes;
            options.MaxSendMessageSize = grpcOptions.MaxSendMessageSizeBytes;
        });

        // Stateless interceptors registered as singletons
        services.AddSingleton<ExceptionInterceptor>();
        services.AddSingleton<LoggingInterceptor>();
        // Scoped: accesses IRequestContext which is per-request
        services.AddScoped<CorrelationIdInterceptor>();

        if (grpcOptions.EnableReflection)
            services.AddGrpcReflection();

        // Registers the grpc-health-v1 service implementation directly.
        // Callers must also invoke MapSharedGrpc() to expose the endpoint.
        if (grpcOptions.EnableHealthCheck)
            services.AddSingleton<HealthServiceImpl>();

        return services;
    }
}
