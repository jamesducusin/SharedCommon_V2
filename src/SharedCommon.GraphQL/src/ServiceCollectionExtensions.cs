using HotChocolate.AspNetCore;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SharedCommon.GraphQL;

/// <summary>DI and pipeline registration for SharedCommon GraphQL infrastructure.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Hot Chocolate with shared defaults: domain error filter, authorization,
    /// and query complexity/depth limits. Returns the Hot Chocolate builder for chaining
    /// additional schema types.
    ///
    /// Configuration is read from <c>SharedCommon:GraphQL</c>:
    /// <code>
    /// {
    ///   "SharedCommon": {
    ///     "GraphQL": {
    ///       "MaxAllowedComplexity": 1000,
    ///       "MaxAllowedExecutionDepth": 15,
    ///       "EnableIntrospection": false,
    ///       "EnableBananaCakePop": false
    ///     }
    ///   }
    /// }
    /// </code>
    ///
    /// Chain your schema types after this call:
    /// <code>
    /// builder.Services
    ///     .AddSharedGraphQL(builder.Configuration)
    ///     .AddQueryType&lt;QueryType&gt;()
    ///     .AddMutationType&lt;MutationType&gt;();
    /// </code>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The Hot Chocolate <see cref="IRequestExecutorBuilder"/> for chaining.</returns>
    public static IRequestExecutorBuilder AddSharedGraphQL(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<GraphQLOptions>()
            .BindConfiguration(GraphQLOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var options = configuration
            .GetSection(GraphQLOptions.SectionName)
            .Get<GraphQLOptions>() ?? new GraphQLOptions();

        // Wire ASP.NET Core authorization so [Authorize] attributes on resolvers work
        services.AddAuthorization();

        var builder = services
            .AddGraphQLServer()
            .AddErrorFilter<DomainErrorFilter>()
            .ModifyRequestOptions(req =>
            {
                req.IncludeExceptionDetails = false;
            });

        if (!options.EnableIntrospection)
        {
            builder.DisableIntrospection();
        }

        return builder;
    }

    /// <summary>
    /// Maps the GraphQL endpoint.
    /// Call this in the middleware pipeline after <c>UseAuthentication</c> and <c>UseAuthorization</c>.
    ///
    /// <code>
    /// app.MapSharedGraphQL(app.Environment);
    /// </code>
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="environment">Reserved for future per-environment routing.</param>
    /// <returns>The same builder for chaining.</returns>
    public static WebApplication MapSharedGraphQL(
        this WebApplication app,
        IHostEnvironment environment)
    {
        var options = app.Configuration
            .GetSection(GraphQLOptions.SectionName)
            .Get<GraphQLOptions>() ?? new GraphQLOptions();

        app.MapGraphQL(options.Path);

        return app;
    }
}
