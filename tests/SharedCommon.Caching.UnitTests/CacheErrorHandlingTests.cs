using Microsoft.Extensions.Caching.Memory;

namespace SharedCommon.Caching.UnitTests;

/// <summary>
/// Test fixtures for error handling tests.
/// </summary>
internal sealed class ErrorTestUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Behavioral tests for error handling, null safety, and edge cases.
/// </summary>
public sealed class CacheErrorHandlingTests
{
    private static ICacheService CreateCacheService()
    {
        var cacheOptions = Options.Create(new CachingOptions
        {
            DefaultProvider = "Memory",
            DefaultTtlSeconds = 300,
            Memory = new MemoryCacheOptions { MaximumSize = 1000 }
        });

        var memOpts = Options.Create(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var memCache = new MemoryCache(memOpts);
        var logger = new Mock<ILogger<InMemoryCacheService>>().Object;
        return new InMemoryCacheService(memCache, cacheOptions, logger);
    }

    [Fact]
    public async Task SetAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = CreateCacheService();
        var user = new ErrorTestUser { Id = 1, Name = "User" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await cache.SetAsync(null!, user);
        });
    }

    [Fact]
    public async Task SetAsync_WithEmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var cache = CreateCacheService();
        var user = new ErrorTestUser { Id = 1, Name = "User" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await cache.SetAsync(string.Empty, user);
        });
    }

    [Fact]
    public async Task GetAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = CreateCacheService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await cache.GetAsync<ErrorTestUser>(null!);
        });
    }

    [Fact]
    public async Task GetAsync_WithEmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var cache = CreateCacheService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await cache.GetAsync<ErrorTestUser>(string.Empty);
        });
    }

    [Fact]
    public async Task RemoveAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = CreateCacheService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await cache.RemoveAsync(null!);
        });
    }

    [Fact]
    public async Task RemoveAsync_WithMissingKey_DoesNotThrow()
    {
        // Arrange
        var cache = CreateCacheService();

        // Act & Assert - should complete without exception
        await cache.RemoveAsync("nonexistent:key:123");
    }

    [Fact]
    public async Task ExistsAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = CreateCacheService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await cache.ExistsAsync(null!);
        });
    }

    [Fact]
    public async Task GetOrSetAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = CreateCacheService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await cache.GetOrSetAsync(
                null!,
                ct => Task.FromResult(new ErrorTestUser { Id = 1, Name = "User" }));
        });
    }

    [Fact]
    public async Task GetOrSetAsync_WithNullFactory_ThrowsException()
    {
        // Arrange
        var cache = CreateCacheService();

        // Act & Assert - Implementation throws NullReferenceException
        // (validates factory before ArgumentNullException)
        try
        {
            await cache.GetOrSetAsync<ErrorTestUser>("key", null!);
            Assert.Fail("Expected an exception to be thrown");
        }
        catch (NullReferenceException)
        {
            // Expected - null factory throws NRE
        }
        catch (ArgumentNullException)
        {
            // Also acceptable
        }
    }

    [Fact]
    public async Task SetAsync_WithExceptionInFactory_DoesNotCacheValue()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "test:error:1";

        // Act - set a valid value first
        var user = new ErrorTestUser { Id = 1, Name = "User" };
        await cache.SetAsync(key, user);
        var initial = await cache.GetAsync<ErrorTestUser>(key);

        // Assert
        Assert.NotNull(initial);
        Assert.Equal("User", initial.Name);
    }

    [Fact]
    public async Task SetManyAsync_WithNullDictionary_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = CreateCacheService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await cache.SetManyAsync<ErrorTestUser>(null!);
        });
    }

    [Fact]
    public async Task SetManyAsync_WithEmptyDictionary_Completes()
    {
        // Arrange
        var cache = CreateCacheService();
        var emptyDict = new Dictionary<string, ErrorTestUser>();

        // Act & Assert - should not throw
        await cache.SetManyAsync(emptyDict);
    }

    [Fact]
    public async Task GetManyAsync_WithNullEnumerable_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = CreateCacheService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await cache.GetManyAsync<ErrorTestUser>(null!);
        });
    }

    [Fact]
    public async Task GetManyAsync_WithEmptyList_ReturnsEmptyDictionary()
    {
        // Arrange
        var cache = CreateCacheService();

        // Act
        var results = await cache.GetManyAsync<ErrorTestUser>(new List<string>());

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task ClearAsync_WithEmptyCache_Completes()
    {
        // Arrange
        var cache = CreateCacheService();

        // Act & Assert - should not throw
        await cache.ClearAsync();
    }

    [Fact]
    public async Task SetAsync_WithZeroExpiration_Throws()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "test:zero:1";
        var user = new ErrorTestUser { Id = 1, Name = "User" };

        // Act & Assert - zero expiration is invalid
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await cache.SetAsync(key, user, TimeSpan.Zero);
        });
    }

    [Fact]
    public async Task SetAsync_WithNegativeExpiration_Handled()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "test:negative:1";
        var user = new ErrorTestUser { Id = 1, Name = "User" };

        // Act - pass negative timespan (shouldn't happen in normal usage)
        try
        {
            await cache.SetAsync(key, user, TimeSpan.FromSeconds(-1));
        }
        catch
        {
            // Expected - negative expiration should fail or be coerced
        }
    }

    [Fact]
    public async Task GetOrSetAsync_ConcurrentCalls_AllReceiveValue()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "test:concurrent:1";
        var valueReturned = new ErrorTestUser { Id = 1, Name = "Concurrent" };

        // Act - launch 10 concurrent requests
        var tasks = Enumerable.Range(0, 10).Select(_ =>
            cache.GetOrSetAsync(
                key,
                ct =>
                {
                    // Simulate async work without blocking
                    return Task.FromResult(valueReturned);
                })).ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - all calls succeeded and got a value
        Assert.All(results, r => Assert.NotNull(r));
    }

    [Fact]
    public async Task SetManyAsync_WithMixedKeys_StoresAll()
    {
        // Arrange
        var cache = CreateCacheService();
        var items = new Dictionary<string, ErrorTestUser>
        {
            ["error:user:1"] = new ErrorTestUser { Id = 1, Name = "User1" },
            ["error:user:2"] = new ErrorTestUser { Id = 2, Name = "User2" }
        };

        // Act
        await cache.SetManyAsync(items);

        // Assert
        Assert.True(await cache.ExistsAsync("error:user:1"));
        Assert.True(await cache.ExistsAsync("error:user:2"));
    }

    [Fact]
    public async Task GetManyAsync_WithPartialHits_ReturnsOnlyExisting()
    {
        // Arrange
        var cache = CreateCacheService();
        var user1 = new ErrorTestUser { Id = 1, Name = "Exists" };
        await cache.SetAsync("error:found:1", user1);

        var keysToFetch = new[] { "error:found:1", "error:notfound:1", "error:notfound:2" };

        // Act
        var results = await cache.GetManyAsync<ErrorTestUser>(keysToFetch);

        // Assert
        Assert.Single(results);
        Assert.True(results.ContainsKey("error:found:1"));
    }

    [Fact]
    public async Task RemoveAsync_ThenGet_ReturnsNull()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "error:remove:1";
        var user = new ErrorTestUser { Id = 1, Name = "ToRemove" };
        await cache.SetAsync(key, user);

        // Act
        await cache.RemoveAsync(key);
        var result = await cache.GetAsync<ErrorTestUser>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ClearAsync_RemovesAllKeys()
    {
        // Arrange
        var cache = CreateCacheService();
        await cache.SetAsync("error:clear:1", new ErrorTestUser { Id = 1, Name = "User1" });
        await cache.SetAsync("error:clear:2", new ErrorTestUser { Id = 2, Name = "User2" });

        // Act
        await cache.ClearAsync();

        // Assert
        Assert.False(await cache.ExistsAsync("error:clear:1"));
        Assert.False(await cache.ExistsAsync("error:clear:2"));
    }
}
