using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SharedCommon.FeatureFlags.Distributed;

/// <summary>
/// Distributed feature flags service for Redis backed percentage-based rollouts and user targeting.
/// Complements the configuration-based <see cref="Microsoft.FeatureManagement.IFeatureManager"/>
/// with dynamic, Redis-backed, percentage-based rollout capabilities.
/// </summary>
/// <remarks>
/// This service is for advanced runtime feature control:
/// - Percentage-based canary deployments (0-100% rollout)
/// - User whitelisting/blacklisting
/// - Dynamic enable/disable without redeployment
///
/// For configuration-based feature flags, use <see cref="Microsoft.FeatureManagement.IFeatureManager"/>.
/// </remarks>
public interface IDistributedFeatureFlagService
{
    /// <summary>
    /// Checks if a feature is enabled for the given context with percentage-based rollout support.
    /// </summary>
    /// <param name="featureKey">The unique feature identifier (e.g., "new-checkout-flow")</param>
    /// <param name="context">Optional context containing user ID and other targeting attributes</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if the feature is enabled for the context; false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when featureKey is null or empty</exception>
    Task<bool> IsEnabledAsync(string featureKey, FeatureFlagContext? context = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current definition of a feature flag.
    /// </summary>
    /// <param name="featureKey">The unique feature identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The feature flag definition, or null if not found</returns>
    /// <exception cref="ArgumentNullException">Thrown when featureKey is null or empty</exception>
    Task<FeatureFlagDefinition?> GetFlagAsync(string featureKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or updates a feature flag definition.
    /// </summary>
    /// <param name="featureKey">The unique feature identifier</param>
    /// <param name="flag">The feature flag configuration</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when featureKey or flag is null</exception>
    Task SetFlagAsync(string featureKey, FeatureFlagDefinition flag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables a feature flag at 100% rollout immediately.
    /// </summary>
    /// <param name="featureKey">The unique feature identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when featureKey is null or empty</exception>
    Task EnableAsync(string featureKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables a feature flag immediately.
    /// </summary>
    /// <param name="featureKey">The unique feature identifier</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when featureKey is null or empty</exception>
    Task DisableAsync(string featureKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a percentage-based rollout for gradual feature deployment (canary pattern).
    /// </summary>
    /// <param name="featureKey">The unique feature identifier</param>
    /// <param name="percentage">Rollout percentage (0-100). 0 = disabled, 100 = enabled for all</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when featureKey is null or empty</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when percentage is not 0-100</exception>
    /// <example>
    /// <code>
    /// // Canary: enable for 5% of users
    /// await featureFlags.SetRolloutPercentageAsync("payment-v2", 5);
    /// // Monitor for issues...
    /// // Increase to 25%
    /// await featureFlags.SetRolloutPercentageAsync("payment-v2", 25);
    /// // Full rollout
    /// await featureFlags.SetRolloutPercentageAsync("payment-v2", 100);
    /// </code>
    /// </example>
    Task SetRolloutPercentageAsync(string featureKey, int percentage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a user to the whitelist (always enabled for that user).
    /// </summary>
    /// <param name="featureKey">The unique feature identifier</param>
    /// <param name="userId">The user ID to whitelist</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when featureKey or userId is null</exception>
    Task AllowUserAsync(string featureKey, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a user from the whitelist.
    /// </summary>
    /// <param name="featureKey">The unique feature identifier</param>
    /// <param name="userId">The user ID to remove from whitelist</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when featureKey or userId is null</exception>
    Task RemoveUserAsync(string featureKey, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active feature flags (useful for admin dashboards and monitoring).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Dictionary mapping feature key to flag definition</returns>
    Task<Dictionary<string, FeatureFlagDefinition>> GetAllFlagsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Feature flag definition with targeting rules and rollout configuration.
/// </summary>
public class FeatureFlagDefinition
{
    /// <summary>
    /// Gets or sets the human-readable name of the feature.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of what the feature does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the feature is enabled. False disables it completely.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the percentage of users who should see this feature (0-100).
    /// 0 = disabled, 50 = 50% of users, 100 = all users.
    /// Determines which users are included via deterministic hashing.
    /// </summary>
    public int RolloutPercentage { get; set; } = 100;

    /// <summary>
    /// Gets or sets the list of user IDs explicitly enabled for this feature (whitelist).
    /// Takes precedence over percentage-based rollout.
    /// </summary>
    public List<string>? EnabledUsers { get; set; }

    /// <summary>
    /// Gets or sets the list of user IDs explicitly disabled for this feature (blacklist).
    /// Takes precedence over percentage-based rollout and enabled users.
    /// </summary>
    public List<string>? DisabledUsers { get; set; }

    /// <summary>
    /// Gets or sets when this feature flag was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when this feature flag was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Context information for feature flag evaluation (user ID, tenant, attributes).
/// </summary>
public class FeatureFlagContext
{
    /// <summary>
    /// Gets or sets the user ID for targeting (required for whitelisting/blacklisting).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for multi-tenant scenarios.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets custom attributes for advanced targeting scenarios.
    /// </summary>
    public Dictionary<string, object>? Attributes { get; set; }
}

/// <summary>
/// Redis-backed implementation of <see cref="IDistributedFeatureFlagService"/>.
/// Provides dynamic, percentage-based feature rollouts with user targeting.
/// </summary>
public sealed class DistributedFeatureFlagService : IDistributedFeatureFlagService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<DistributedFeatureFlagService> _logger;
    private const string PREFIX = "featureflag:";

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedFeatureFlagService"/> class.
    /// </summary>
    /// <param name="redis">Redis connection multiplexer for distributed flag storage</param>
    /// <param name="logger">Logger for diagnostic output</param>
    /// <exception cref="ArgumentNullException">Thrown when redis or logger is null</exception>
    public DistributedFeatureFlagService(
        IConnectionMultiplexer redis,
        ILogger<DistributedFeatureFlagService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(
        string featureKey,
        FeatureFlagContext? context = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(featureKey)) throw new ArgumentNullException(nameof(featureKey));

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{PREFIX}{featureKey}";
            var flagJson = await db.StringGetAsync(key);

            if (!flagJson.HasValue)
            {
                _logger.LogDebug("Feature flag not found: {FeatureKey}", featureKey);
                return false;
            }

            var flag = System.Text.Json.JsonSerializer.Deserialize<FeatureFlagDefinition>(flagJson.ToString())
                ?? throw new InvalidOperationException($"Invalid feature flag JSON: {featureKey}");

            var enabled = EvaluateFlag(flag, context);
            
            _logger.LogDebug(
                "Feature flag evaluated: {FeatureKey}, Enabled={Enabled}, UserId={UserId}",
                featureKey, enabled, context?.UserId ?? "anonymous");

            return enabled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating feature flag: {FeatureKey}", featureKey);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<FeatureFlagDefinition?> GetFlagAsync(
        string featureKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(featureKey)) throw new ArgumentNullException(nameof(featureKey));

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{PREFIX}{featureKey}";
            var flagJson = await db.StringGetAsync(key);

            if (!flagJson.HasValue)
                return null;

            return System.Text.Json.JsonSerializer.Deserialize<FeatureFlagDefinition>(flagJson.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature flag: {FeatureKey}", featureKey);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetFlagAsync(
        string featureKey,
        FeatureFlagDefinition flag,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(featureKey)) throw new ArgumentNullException(nameof(featureKey));
        if (flag == null) throw new ArgumentNullException(nameof(flag));

        try
        {
            var db = _redis.GetDatabase();
            var key = $"{PREFIX}{featureKey}";
            flag.UpdatedAt = DateTime.UtcNow;
            var json = System.Text.Json.JsonSerializer.Serialize(flag);

            await db.StringSetAsync(key, json);
            _logger.LogInformation("Feature flag set: {FeatureKey}", featureKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting feature flag: {FeatureKey}", featureKey);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task EnableAsync(string featureKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(featureKey)) throw new ArgumentNullException(nameof(featureKey));

        var flag = await GetFlagAsync(featureKey, cancellationToken) ?? new FeatureFlagDefinition
        {
            Name = featureKey,
            Enabled = true,
            RolloutPercentage = 100
        };

        flag.Enabled = true;
        flag.RolloutPercentage = 100;

        await SetFlagAsync(featureKey, flag, cancellationToken);
        _logger.LogInformation("Feature flag enabled: {FeatureKey}", featureKey);
    }

    /// <inheritdoc />
    public async Task DisableAsync(string featureKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(featureKey)) throw new ArgumentNullException(nameof(featureKey));

        var flag = await GetFlagAsync(featureKey, cancellationToken) ?? new FeatureFlagDefinition
        {
            Name = featureKey,
            Enabled = false
        };

        flag.Enabled = false;

        await SetFlagAsync(featureKey, flag, cancellationToken);
        _logger.LogInformation("Feature flag disabled: {FeatureKey}", featureKey);
    }

    /// <inheritdoc />
    public async Task SetRolloutPercentageAsync(
        string featureKey,
        int percentage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(featureKey)) throw new ArgumentNullException(nameof(featureKey));
        if (percentage < 0 || percentage > 100)
            throw new ArgumentOutOfRangeException(nameof(percentage), "Must be 0-100");

        var flag = await GetFlagAsync(featureKey, cancellationToken) ?? new FeatureFlagDefinition
        {
            Name = featureKey,
            Enabled = percentage > 0
        };

        flag.RolloutPercentage = percentage;
        flag.Enabled = percentage > 0;

        await SetFlagAsync(featureKey, flag, cancellationToken);
        _logger.LogInformation("Feature flag rollout set to {Percentage}%: {FeatureKey}", percentage, featureKey);
    }

    /// <inheritdoc />
    public async Task AllowUserAsync(
        string featureKey,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(featureKey)) throw new ArgumentNullException(nameof(featureKey));
        if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));

        var flag = await GetFlagAsync(featureKey, cancellationToken) ?? new FeatureFlagDefinition
        {
            Name = featureKey,
            EnabledUsers = new List<string>()
        };

        if (flag.EnabledUsers == null)
            flag.EnabledUsers = new List<string>();

        if (!flag.EnabledUsers.Contains(userId))
        {
            flag.EnabledUsers.Add(userId);
            await SetFlagAsync(featureKey, flag, cancellationToken);
            _logger.LogInformation("User {UserId} whitelisted for feature: {FeatureKey}", userId, featureKey);
        }
    }

    /// <inheritdoc />
    public async Task RemoveUserAsync(
        string featureKey,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(featureKey)) throw new ArgumentNullException(nameof(featureKey));
        if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));

        var flag = await GetFlagAsync(featureKey, cancellationToken);
        if (flag?.EnabledUsers != null && flag.EnabledUsers.Contains(userId))
        {
            flag.EnabledUsers.Remove(userId);
            await SetFlagAsync(featureKey, flag, cancellationToken);
            _logger.LogInformation("User {UserId} removed from feature: {FeatureKey}", userId, featureKey);
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, FeatureFlagDefinition>> GetAllFlagsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            if (endpoints.Length == 0)
                return new Dictionary<string, FeatureFlagDefinition>();

            var server = _redis.GetServer(endpoints[0]);
            var flags = new Dictionary<string, FeatureFlagDefinition>();

            if (server == null)
                return flags;

            await foreach (var key in server.KeysAsync(pattern: $"{PREFIX}*"))
            {
                var db = _redis.GetDatabase();
                var value = await db.StringGetAsync(key);

                if (value.HasValue)
                {
                    var flagName = key.ToString().Replace(PREFIX, "");
                    var flag = System.Text.Json.JsonSerializer.Deserialize<FeatureFlagDefinition>(value.ToString());
                    if (flag != null)
                        flags[flagName] = flag;
                }
            }

            return flags;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all feature flags");
            return new Dictionary<string, FeatureFlagDefinition>();
        }
    }

    /// <summary>
    /// Evaluates whether a feature flag is enabled for the given context.
    /// </summary>
    /// <param name="flag">The feature flag definition</param>
    /// <param name="context">Optional context for evaluation</param>
    /// <returns>True if the feature is enabled for the context</returns>
    private static bool EvaluateFlag(FeatureFlagDefinition flag, FeatureFlagContext? context)
    {
        // Feature is explicitly disabled globally
        if (!flag.Enabled)
            return false;

        // User is explicitly blacklisted (highest priority)
        if (context?.UserId != null && flag.DisabledUsers?.Contains(context.UserId) == true)
            return false;

        // User is explicitly whitelisted
        if (context?.UserId != null && flag.EnabledUsers?.Contains(context.UserId) == true)
            return true;

        // Percentage-based rollout (deterministic based on user hash)
        if (flag.RolloutPercentage < 100)
        {
            var hash = GetUserHash(context?.UserId ?? "anonymous");
            var percentage = hash % 100;
            return percentage < flag.RolloutPercentage;
        }

        return true;
    }

    /// <summary>
    /// Gets a deterministic hash for a user for consistent percentage-based rollouts.
    /// Same user always gets the same result for a given percentage.
    /// </summary>
    /// <param name="userId">The user ID to hash</param>
    /// <returns>Hash value (0-99)</returns>
    private static int GetUserHash(string userId)
    {
        unchecked
        {
            int hash = 17;
            foreach (char c in userId)
            {
                hash = hash * 31 + c;
            }
            return Math.Abs(hash) % 100;
        }
    }
}
