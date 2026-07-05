using Grpc.HealthCheck;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace SharedCommon.Grpc;

/// <summary>Endpoint mapping extensions for SharedCommon gRPC services.</summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Maps the gRPC health check service and, when reflection is enabled in options,
    /// the gRPC reflection service (restricted to Development by default).
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="environment">Optional host environment used to guard reflection mapping.</param>
    public static IEndpointRouteBuilder MapSharedGrpc(
        this IEndpointRouteBuilder endpoints,
        IWebHostEnvironment? environment = null)
    {
        var grpcOptions = endpoints.ServiceProvider
            .GetRequiredService<IOptions<GrpcOptions>>()
            .Value;

        if (grpcOptions.EnableHealthCheck)
            endpoints.MapGrpcService<HealthServiceImpl>();

        if (grpcOptions.EnableReflection && environment?.IsDevelopment() != false)
            endpoints.MapGrpcReflectionService();

        return endpoints;
    }
}
