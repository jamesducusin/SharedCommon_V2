using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;
using SharedCommon.Resiliency.RateLimiting;

namespace SharedCommon.Resiliency.UnitTests.RateLimiting;

/// <summary>
/// Unit tests for <see cref="DistributedRateLimiter"/>.
/// </summary>
public class DistributedRateLimiterTests : IAsyncLifetime
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis = new();
    private readonly Mock<IDatabase> _mockDatabase = new();
    private readonly Mock<ILogger<DistributedRateLimiter>> _mockLogger = new();
    private RateLimiterOptions _options = null!;
    private DistributedRateLimiter _rateLimiter = null!;

    public Task InitializeAsync()
    {
        _options = new RateLimiterOptions
        {
            Limit = 100,
            WindowSeconds = 60,
            Enabled = true,
            StrictMode = false
        };

        _mockRedis
            .Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDatabase.Object);

        _rateLimiter = new DistributedRateLimiter(_mockRedis.Object, _options, _mockLogger.Object);
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TryAcquireAsync_WithValidKey_ReturnsResult()
    {
        // Arrange
        var key = "user:123";
        SetupRedisForAllowedRequest();

        // Act
        var result = await _rateLimiter.TryAcquireAsync(key);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Allowed);
    }

    [Fact]
    public async Task TryAcquireAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _rateLimiter.TryAcquireAsync(null!));
    }

    [Fact]
    public async Task TryAcquireAsync_WithEmptyKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _rateLimiter.TryAcquireAsync(""));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task TryAcquireAsync_WithInvalidTokenCount_ThrowsArgumentOutOfRangeException(int tokens)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            _rateLimiter.TryAcquireAsync("user:123", tokens));
    }

    [Fact]
    public async Task TryAcquireAsync_WhenRateLimited_ReturnsDenied()
    {
        // Arrange
        var key = "user:123";
        SetupRedisForRateLimitedRequest();

        // Act
        var result = await _rateLimiter.TryAcquireAsync(key);

        // Assert
        Assert.False(result.Allowed);
        Assert.Equal(0, result.TokensRemaining);
        Assert.NotNull(result.RetryAfter);
    }

    [Fact]
    public async Task TryAcquireAsync_OnRedisError_FailsOpen()
    {
        // Arrange
        var key = "user:123";
        _mockDatabase
            .Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.None, "Redis unavailable"));

        // Act
        var result = await _rateLimiter.TryAcquireAsync(key);

        // Assert
        Assert.True(result.Allowed); // Fails open
        Assert.True(result.ErrorOccurred);
    }

    [Fact]
    public async Task TryAcquireAsync_OnRedisErrorInStrictMode_ThrowsException()
    {
        // Arrange
        _options.StrictMode = true;
        var key = "user:123";
        _mockDatabase
            .Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.None, "Redis unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<RedisConnectionException>(() => _rateLimiter.TryAcquireAsync(key));
    }

    [Fact]
    public async Task ResetAsync_WithValidKey_DeletesCounters()
    {
        // Arrange
        var key = "user:123";
        _mockDatabase
            .Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult(2L));

        // Act
        await _rateLimiter.ResetAsync(key);

        // Assert
        _mockDatabase.Verify(
            d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _rateLimiter.ResetAsync(null!));
    }

    [Fact]
    public async Task GetStatusAsync_WithActiveLimit_ReturnsStatus()
    {
        // Arrange
        var key = "user:123";
        SetupRedisForStatusCheck();

        // Act
        var status = await _rateLimiter.GetStatusAsync(key);

        // Assert
        Assert.NotNull(status);
        Assert.Equal(key, status.Key);
        Assert.True(status.TokensRemaining > 0);
        Assert.True(status.WindowPercentUsed >= 0);
    }

    [Fact]
    public async Task GetStatusAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _rateLimiter.GetStatusAsync(null!));
    }

    [Fact]
    public async Task GetStatusAsync_WithNoActiveLimit_ReturnsFullLimit()
    {
        // Arrange
        var key = "user:456";
        _mockDatabase
            .Setup(d => d.StringGetAsync("ratelimit:user:456", It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult((RedisValue?)null));
        _mockDatabase
            .Setup(d => d.StringGetAsync("ratelimit:user:456:window", It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult((RedisValue?)null));

        // Act
        var status = await _rateLimiter.GetStatusAsync(key);

        // Assert
        Assert.Equal(_options.Limit, status.TokensRemaining);
        Assert.Equal(0, status.WindowPercentUsed);
    }

    [Fact]
    public async Task TryAcquireAsync_MultipleRequests_DecreasesTokens()
    {
        // Arrange
        var key = "user:789";
        var callCount = 0;
        
        _mockDatabase
            .Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>()))
            .Returns(() => Task.FromResult((RedisResult)GetMockLuaResult(++callCount)));

        // Act
        var result1 = await _rateLimiter.TryAcquireAsync(key);
        var result2 = await _rateLimiter.TryAcquireAsync(key);

        // Assert
        Assert.True(result1.Allowed);
        Assert.True(result2.Allowed);
        Assert.NotEqual(result1.TokensRemaining, result2.TokensRemaining);
    }

    private void SetupRedisForAllowedRequest()
    {
        var mockResult = new RedisResult[4];
        mockResult[0] = RedisResult.Create(1L);  // allowed = 1
        mockResult[1] = RedisResult.Create(99L); // tokens remaining
        mockResult[2] = RedisResult.Create(DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeSeconds());
        mockResult[3] = RedisResult.Create(0L);  // retry after

        _mockDatabase
            .Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>()))
            .Returns(Task.FromResult((RedisResult)mockResult));
    }

    private void SetupRedisForRateLimitedRequest()
    {
        var mockResult = new RedisResult[4];
        mockResult[0] = RedisResult.Create(0L);  // allowed = 0
        mockResult[1] = RedisResult.Create(0L);  // tokens remaining
        mockResult[2] = RedisResult.Create(DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeSeconds());
        mockResult[3] = RedisResult.Create(30L); // retry after 30 seconds

        _mockDatabase
            .Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>()))
            .Returns(Task.FromResult((RedisResult)mockResult));
    }

    private void SetupRedisForStatusCheck()
    {
        _mockDatabase
            .Setup(d => d.StringGetAsync("ratelimit:user:123", It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult((RedisValue?)"75"));
        
        _mockDatabase
            .Setup(d => d.StringGetAsync("ratelimit:user:123:window", It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult((RedisValue?)DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));
    }

    private RedisResult GetMockLuaResult(int callCount)
    {
        var remaining = 100 - callCount;
        var mockResult = new RedisResult[4];
        mockResult[0] = RedisResult.Create(1L);
        mockResult[1] = RedisResult.Create((long)remaining);
        mockResult[2] = RedisResult.Create(DateTimeOffset.UtcNow.AddSeconds(60).ToUnixTimeSeconds());
        mockResult[3] = RedisResult.Create(0L);
        return mockResult;
    }
}
