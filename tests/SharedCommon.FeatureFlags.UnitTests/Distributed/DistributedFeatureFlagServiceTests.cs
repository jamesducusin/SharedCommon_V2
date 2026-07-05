using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SharedCommon.FeatureFlags.Distributed;

namespace SharedCommon.FeatureFlags.UnitTests.Distributed;

/// <summary>
/// Unit tests for <see cref="DistributedFeatureFlagService"/>.
/// </summary>
public class DistributedFeatureFlagServiceTests
{
    private readonly Mock<StackExchange.Redis.IConnectionMultiplexer> _mockRedis = new();
    private readonly Mock<StackExchange.Redis.IDatabase> _mockDatabase = new();
    private readonly Mock<ILogger<DistributedFeatureFlagService>> _mockLogger = new();
    private DistributedFeatureFlagService _service = null!;

    public DistributedFeatureFlagServiceTests()
    {
        _mockRedis
            .Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDatabase.Object);

        _service = new DistributedFeatureFlagService(_mockRedis.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task IsEnabledAsync_WithDisabledFlag_ReturnsFalse()
    {
        // Arrange
        var flag = new FeatureFlagDefinition { Name = "test", Enabled = false };
        var flagJson = System.Text.Json.JsonSerializer.Serialize(flag);
        
        _mockDatabase
            .Setup(d => d.StringGetAsync(It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<StackExchange.Redis.CommandFlags>()))
            .Returns(Task.FromResult((StackExchange.Redis.RedisValue?)flagJson));

        // Act
        var result = await _service.IsEnabledAsync("test-flag");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsEnabledAsync_WithEnabledFlag_ReturnsTrue()
    {
        // Arrange
        var flag = new FeatureFlagDefinition 
        { 
            Name = "test", 
            Enabled = true,
            RolloutPercentage = 100 
        };
        var flagJson = System.Text.Json.JsonSerializer.Serialize(flag);
        
        _mockDatabase
            .Setup(d => d.StringGetAsync(It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<StackExchange.Redis.CommandFlags>()))
            .Returns(Task.FromResult((StackExchange.Redis.RedisValue?)flagJson));

        // Act
        var result = await _service.IsEnabledAsync("test-flag");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsEnabledAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.IsEnabledAsync(null!));
    }

    [Fact]
    public async Task IsEnabledAsync_WithEmptyKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.IsEnabledAsync(""));
    }

    [Fact]
    public async Task IsEnabledAsync_WithWhitelistedUser_ReturnsTrue()
    {
        // Arrange
        var flag = new FeatureFlagDefinition
        {
            Name = "test",
            Enabled = true,
            RolloutPercentage = 0, // Disabled by percentage
            EnabledUsers = new() { "user-123" }
        };
        var flagJson = System.Text.Json.JsonSerializer.Serialize(flag);
        
        _mockDatabase
            .Setup(d => d.StringGetAsync(It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<StackExchange.Redis.CommandFlags>()))
            .Returns(Task.FromResult((StackExchange.Redis.RedisValue?)flagJson));

        var context = new FeatureFlagContext { UserId = "user-123" };

        // Act
        var result = await _service.IsEnabledAsync("test-flag", context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsEnabledAsync_WithBlacklistedUser_ReturnsFalse()
    {
        // Arrange
        var flag = new FeatureFlagDefinition
        {
            Name = "test",
            Enabled = true,
            RolloutPercentage = 100, // Enabled by percentage
            DisabledUsers = new() { "user-456" }
        };
        var flagJson = System.Text.Json.JsonSerializer.Serialize(flag);
        
        _mockDatabase
            .Setup(d => d.StringGetAsync(It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<StackExchange.Redis.CommandFlags>()))
            .Returns(Task.FromResult((StackExchange.Redis.RedisValue?)flagJson));

        var context = new FeatureFlagContext { UserId = "user-456" };

        // Act
        var result = await _service.IsEnabledAsync("test-flag", context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsEnabledAsync_WithCanaryRollout_ReturnsConsistentResults()
    {
        // Arrange
        var flag = new FeatureFlagDefinition
        {
            Name = "test",
            Enabled = true,
            RolloutPercentage = 10 // 10% canary
        };
        var flagJson = System.Text.Json.JsonSerializer.Serialize(flag);
        
        _mockDatabase
            .Setup(d => d.StringGetAsync(It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<StackExchange.Redis.CommandFlags>()))
            .Returns(Task.FromResult((StackExchange.Redis.RedisValue?)flagJson));

        // Act - same user should get same result multiple times
        var context = new FeatureFlagContext { UserId = "user-123" };
        var result1 = await _service.IsEnabledAsync("test-flag", context);
        var result2 = await _service.IsEnabledAsync("test-flag", context);

        // Assert
        Assert.Equal(result1, result2); // Consistent for same user
    }

    [Fact]
    public async Task EnableAsync_SetsRolloutTo100Percent()
    {
        // Arrange
        _mockDatabase
            .Setup(d => d.StringGetAsync(It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<StackExchange.Redis.CommandFlags>()))
            .Returns(Task.FromResult((StackExchange.Redis.RedisValue?)null));
        
        _mockDatabase
            .Setup(d => d.StringSetAsync(It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<StackExchange.Redis.RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<StackExchange.Redis.CommandFlags>()))
            .Returns(Task.FromResult(true));

        // Act
        await _service.EnableAsync("test-flag");

        // Assert
        _mockDatabase.Verify(
            d => d.StringSetAsync(It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<StackExchange.Redis.RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<StackExchange.Redis.CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task DisableAsync_SetsEnabledToFalse()
    {
        // Arrange
        _mockDatabase
            .Setup(d => d.StringGetAsync(It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<StackExchange.Redis.CommandFlags>()))
            .Returns(Task.FromResult((StackExchange.Redis.RedisValue?)null));
        
        _mockDatabase
            .Setup(d => d.StringSetAsync(It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<StackExchange.Redis.RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<StackExchange.Redis.CommandFlags>()))
            .Returns(Task.FromResult(true));

        // Act
        await _service.DisableAsync("test-flag");

        // Assert
        _mockDatabase.Verify(
            d => d.StringSetAsync(It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<StackExchange.Redis.RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<StackExchange.Redis.CommandFlags>()),
            Times.Once);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task SetRolloutPercentageAsync_WithInvalidPercentage_ThrowsArgumentOutOfRangeException(int percentage)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.SetRolloutPercentageAsync("test-flag", percentage));
    }

    [Fact]
    public async Task AllowUserAsync_AddsUserToWhitelist()
    {
        // Arrange
        _mockDatabase
            .Setup(d => d.StringGetAsync(It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<StackExchange.Redis.CommandFlags>()))
            .Returns(Task.FromResult((StackExchange.Redis.RedisValue?)null));
        
        _mockDatabase
            .Setup(d => d.StringSetAsync(It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<StackExchange.Redis.RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<StackExchange.Redis.CommandFlags>()))
            .Returns(Task.FromResult(true));

        // Act
        await _service.AllowUserAsync("test-flag", "user-123");

        // Assert
        _mockDatabase.Verify(
            d => d.StringSetAsync(It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<StackExchange.Redis.RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<StackExchange.Redis.CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task AllowUserAsync_WithNullUserId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.AllowUserAsync("test-flag", null!));
    }

    [Fact]
    public async Task GetFlagAsync_WithNonexistentFlag_ReturnsNull()
    {
        // Arrange
        _mockDatabase
            .Setup(d => d.StringGetAsync(It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<StackExchange.Redis.CommandFlags>()))
            .Returns(Task.FromResult((StackExchange.Redis.RedisValue?)null));

        // Act
        var result = await _service.GetFlagAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }
}
