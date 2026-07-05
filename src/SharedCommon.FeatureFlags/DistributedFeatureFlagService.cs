using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace SharedCommon.FeatureFlags;

/// <summary>
/// Distributed feature flags service using Redis.
/// Supports boolean toggles, percentage rollouts, and user/context-based targeting.
/// </summary>
public class DistributedFeatureFlagService : IFeatureFlagService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<DistributedFeatureFlagService> _logger;
    private const string PREFIX = "featureflag:";

    public DistributedFeatureFlagService(
        IConnectionMultiplexer redis,
        ILogger<DistributedFeatureFlagService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if a feature is enabled for the current context.
    /// Supports percentage rollouts and user targeting.
    /// </summary>
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
                _logger.LogWarning("Feature flag not found: {FeatureKey}", featureKey);
                return false; // Default to disabled if not found
            }

            var flag = System.Text.Json.JsonSerializer.Deserialize<FeatureFlagDefinition>(flagJson.ToString())
                ?? throw new InvalidOperationException($"Invalid feature flag JSON: {featureKey}");

            return EvaluateFlag(flag, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating feature flag: {FeatureKey}", featureKey);
            return false; // Fail closed - don't enable unknown features on error
        }
    }

    /// <summary>
    /// Gets the current definition of a feature flag.
    /// </summary>
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

    /// <summary>
    /// Sets or updates a feature flag definition.
    /// </summary>
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

    /// <summary>
    /// Enables a feature flag immediately (100% rollout).
    /// </summary>
    public async Task EnableAsync(string featureKey, CancellationToken cancellationToken = default)
    {
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

    /// <summary>
    /// Disables a feature flag immediately.
    /// </summary>
    public async Task DisableAsync(string featureKey, CancellationToken cancellationToken = default)
    {
        var flag = await GetFlagAsync(featureKey, cancellationToken) ?? new FeatureFlagDefinition
        {
            Name = featureKey,
            Enabled = false
        };

        flag.Enabled = false;

        await SetFlagAsync(featureKey, flag, cancellationToken);
        _logger.LogInformation("Feature flag disabled: {FeatureKey}", featureKey);
    }

    /// <summary>
    /// Sets a percentage-based rollout (canary deployment pattern).
    /// Example: 10% means 10% of users see the feature.
    /// </summary>
    public async Task SetRolloutPercentageAsync(
        string featureKey,
        int percentage,
        CancellationToken cancellationToken = default)
    {
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

    /// <summary>
    /// Adds a user to the whitelist (always enabled for that user).
    /// </summary>
    public async Task AllowUserAsync(
        string featureKey,
        string userId,
        CancellationToken cancellationToken = default)
    {
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

    /// <summary>
    /// Removes a user from the whitelist.
    /// </summary>
    public async Task RemoveUserAsync(
        string featureKey,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));

        var flag = await GetFlagAsync(featureKey, cancellationToken);
        if (flag?.EnabledUsers != null && flag.EnabledUsers.Contains(userId))
        {
            flag.EnabledUsers.Remove(userId);
            await SetFlagAsync(featureKey, flag, cancellationToken);
            _logger.LogInformation("User {UserId} removed from feature: {FeatureKey}", userId, featureKey);
        }
    }

    /// <summary>
    /// Gets all feature flags (useful for admin dashboards).
    /// </summary>
    public async Task<Dictionary<string, FeatureFlagDefinition>> GetAllFlagsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var flags = new Dictionary<string, FeatureFlagDefinition>();

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

    private bool EvaluateFlag(FeatureFlagDefinition flag, FeatureFlagContext? context)
    {
        // Feature is explicitly disabled
        if (!flag.Enabled)
            return false;

        // User is explicitly whitelisted
        if (context?.UserId != null && flag.EnabledUsers?.Contains(context.UserId) == true)
            return true;

        // User is explicitly blacklisted
        if (context?.UserId != null && flag.DisabledUsers?.Contains(context.UserId) == true)
            return false;

        // Percentage rollout (consistent based on user ID)
        if (flag.RolloutPercentage < 100)
        {
            var hash = GetUserHash(context?.UserId ?? "anonymous");
            var percentage = hash % 100;
            return percentage < flag.RolloutPercentage;
        }

        return true;
    }

    /// <summary>
    /// Gets a consistent hash for a user to enable deterministic rollouts.
    /// Same user always gets same result for percentage-based flags.
    /// </summary>
    private int GetUserHash(string userId)
    {
        unchecked
        {
            int hash = 17;
            foreach (char c in userId)
            {
                hash = hash * 31 + c;
            }
            return Math.Abs(hash);
        }
    }
}

public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string featureKey, FeatureFlagContext? context = null, CancellationToken cancellationToken = default);
    Task<FeatureFlagDefinition?> GetFlagAsync(string featureKey, CancellationToken cancellationToken = default);
    Task SetFlagAsync(string featureKey, FeatureFlagDefinition flag, CancellationToken cancellationToken = default);
    Task EnableAsync(string featureKey, CancellationToken cancellationToken = default);
    Task DisableAsync(string featureKey, CancellationToken cancellationToken = default);
    Task SetRolloutPercentageAsync(string featureKey, int percentage, CancellationToken cancellationToken = default);
    Task AllowUserAsync(string featureKey, string userId, CancellationToken cancellationToken = default);
    Task RemoveUserAsync(string featureKey, string userId, CancellationToken cancellationToken = default);
    Task<Dictionary<string, FeatureFlagDefinition>> GetAllFlagsAsync(CancellationToken cancellationToken = default);
}

public class FeatureFlagDefinition
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Enabled { get; set; }
    public int RolloutPercentage { get; set; } = 100;
    public List<string>? EnabledUsers { get; set; }
    public List<string>? DisabledUsers { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class FeatureFlagContext
{
    public string? UserId { get; set; }
    public string? TenantId { get; set; }
    public Dictionary<string, object>? Attributes { get; set; }
}
