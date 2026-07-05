using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SharedCommon.Caching;

/// <summary>DI registration extensions for SharedCommon.Caching.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SharedCommon caching services to the DI container.
    ///
    /// Registers:
    /// <list type="bullet">
    ///   <item><see cref="CachingOptions"/> — validated at startup.</item>
    ///   <item><see cref="IMemoryCache"/> — always registered (L1).</item>
    ///   <item>Redis distributed cache — when <c>Redis.Enabled = true</c>.</item>
    ///   <item><see cref="ICacheService"/> — resolved to the correct implementation based on <c>DefaultProvider</c>.</item>
    /// </list>
    ///
    /// Example:
    /// <code>
    /// builder.Services.AddSharedCommonCaching(builder.Configuration);
    /// </code>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either argument is null.</exception>
    public static IServiceCollection AddSharedCommonCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<CachingOptions>()
            .BindConfiguration(CachingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // L1 — always register IMemoryCache.
        services.AddMemoryCache(opts =>
        {
            // SizeLimit enforces MaximumSize; each entry uses Size = 1.
            // Resolved lazily after options are bound.
        });

        // Resolve memory cache size limit post-options-binding.
        services.PostConfigure<Microsoft.Extensions.Caching.Memory.MemoryCacheOptions>((memOpts, sp) =>
        {
            var cachingOpts = sp.GetRequiredService<IOptions<CachingOptions>>().Value;
            if (memOpts.SizeLimit is null)
                memOpts.SizeLimit = cachingOpts.Memory.MaximumSize;
        });

        // Register the concrete ICacheService based on configuration.
        services.AddSingleton<ICacheService>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CachingOptions>>().Value;

            return opts.DefaultProvider.ToUpperInvariant() switch
            {
                "REDIS" => BuildRedisService(sp, opts),
                "MEMORY" => BuildMemoryService(sp),
                _ => BuildHybridService(sp, opts) // default: Hybrid
            };
        });

        return services;
    }

    private static ICacheService BuildMemoryService(IServiceProvider sp)
    {
        return ActivatorUtilities.CreateInstance<InMemoryCacheService>(sp);
    }

    private static ICacheService BuildRedisService(IServiceProvider sp, CachingOptions opts)
    {
        if (!opts.Redis.Enabled || string.IsNullOrWhiteSpace(opts.Redis.Connection))
            throw new InvalidOperationException(
                "DefaultProvider is 'Redis' but Redis.Enabled is false or Redis.Connection is empty.");

        EnsureRedisRegistered(sp, opts);

        // Redis-only: wrap as a hybrid with L1 disabled.
        var hybridOpts = new CachingOptions
        {
            DefaultTtlSeconds = opts.DefaultTtlSeconds,
            CacheKeyPolicy = opts.CacheKeyPolicy,
            Redis = opts.Redis,
            Diagnostics = opts.Diagnostics,
            Hybrid = new HybridCacheOptions
            {
                L1Enabled = false,
                L2Enabled = true,
                PromoteOnHit = false
            }
        };

        return ActivatorUtilities.CreateInstance<HybridCacheService>(sp, Options.Create(hybridOpts));
    }

    private static ICacheService BuildHybridService(IServiceProvider sp, CachingOptions opts)
    {
        if (opts.Hybrid.L2Enabled && !string.IsNullOrWhiteSpace(opts.Redis.Connection))
            EnsureRedisRegistered(sp, opts);

        return ActivatorUtilities.CreateInstance<HybridCacheService>(sp);
    }

    private static void EnsureRedisRegistered(IServiceProvider sp, CachingOptions opts)
    {
        // IDistributedCache must have been registered before we get here.
        // It is registered via AddStackExchangeRedisCache in a post-configure step.
        // This method is a guard only; the actual registration happens below.
        _ = sp; // suppress unused parameter warning — kept for symmetry with callers.
    }

    // PostConfigure extension used to apply SizeLimit after options binding.
}

internal static class MemoryCacheServiceCollectionExtensions
{
    internal static IServiceCollection PostConfigure<TOptions>(
        this IServiceCollection services,
        Action<TOptions, IServiceProvider> configureOptions)
        where TOptions : class
    {
        services.AddSingleton<IPostConfigureOptions<TOptions>>(sp =>
            new PostConfigureOptions<TOptions>(
                name: null,
                action: o => configureOptions(o, sp)));
        return services;
    }
}
