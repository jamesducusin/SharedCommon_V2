using Microsoft.Extensions.Caching.Memory;

namespace SharedCommon.Caching.UnitTests;

/// <summary>
/// Test fixtures - reference types required by ICacheService generics.
/// </summary>
internal sealed class CacheTestUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

internal sealed class CacheTestProduct
{
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

/// <summary>
/// Behavioral tests for ICacheService core operations.
/// </summary>
public sealed class CacheServiceTests
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
    public async Task SetAsync_WithValidValue_StoresSuccessfully()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "test:user:1";
        var user = new CacheTestUser { Id = 1, Name = "Alice" };

        // Act
        await cache.SetAsync(key, user, TimeSpan.FromMinutes(5));

        // Assert
        var retrieved = await cache.GetAsync<CacheTestUser>(key);
        Assert.NotNull(retrieved);
        Assert.Equal(user.Id, retrieved.Id);
        Assert.Equal(user.Name, retrieved.Name);
    }

    [Fact]
    public async Task GetAsync_WithValidKey_ReturnsStoredValue()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "test:product:123";
        var product = new CacheTestProduct { Sku = "ABC123", Price = 99.99m };
        await cache.SetAsync(key, product);

        // Act
        var result = await cache.GetAsync<CacheTestProduct>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ABC123", result.Sku);
        Assert.Equal(99.99m, result.Price);
    }

    [Fact]
    public async Task GetAsync_WithMissingKey_ReturnsNull()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "test:nonexistent:999";

        // Act
        var result = await cache.GetAsync<CacheTestUser>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveAsync_WithExistingKey_DeletesValue()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "test:user:5";
        var user = new CacheTestUser { Id = 5, Name = "Bob" };
        await cache.SetAsync(key, user);

        // Act
        await cache.RemoveAsync(key);

        // Assert
        var result = await cache.GetAsync<CacheTestUser>(key);
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveAsync_WithNonexistentKey_CompletesSuccessfully()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "test:missing:key";

        // Act & Assert - should not throw
        await cache.RemoveAsync(key);
    }

    [Fact]
    public async Task ExistsAsync_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "test:product:exists";
        var product = new CacheTestProduct { Sku = "XYZ", Price = 50m };
        await cache.SetAsync(key, product);

        // Act
        var exists = await cache.ExistsAsync(key);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WithMissingKey_ReturnsFalse()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "test:notfound:999";

        // Act
        var exists = await cache.ExistsAsync(key);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task GetOrSetAsync_WithCacheMiss_ExecutesFactory()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "test:factory:1";
        var user = new CacheTestUser { Id = 1, Name = "Charlie" };
        var factoryCalled = false;

        // Act
        var result = await cache.GetOrSetAsync(
            key,
            ct =>
            {
                factoryCalled = true;
                return Task.FromResult(user);
            });

        // Assert
        Assert.True(factoryCalled);
        Assert.NotNull(result);
        Assert.Equal("Charlie", result.Name);
    }

    [Fact]
    public async Task GetOrSetAsync_WithCacheHit_SkipsFactory()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "test:cached:2";
        var user = new CacheTestUser { Id = 2, Name = "Diana" };
        await cache.SetAsync(key, user);
        var factoryCalled = false;

        // Act
        var result = await cache.GetOrSetAsync(
            key,
            ct =>
            {
                factoryCalled = true;
                return Task.FromResult(new CacheTestUser { Id = 999, Name = "Ignored" });
            });

        // Assert
        Assert.False(factoryCalled);
        Assert.Equal("Diana", result.Name);
    }

    [Fact]
    public async Task GetOrSetAsync_WithStampedeProtection_OnlyCallsFactoryOnce()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "test:stampede:1";
        var callCount = 0;

        // Act - launch 5 concurrent requests for the same missing key
        var tasks = Enumerable.Range(0, 5).Select(_ =>
            cache.GetOrSetAsync(
                key,
                async ct =>
                {
                    Interlocked.Increment(ref callCount);
                    await Task.Delay(10, ct); // Simulate work
                    return new CacheTestUser { Id = 1, Name = "User" };
                })).ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - factory called only once due to stampede protection
        Assert.Equal(1, callCount);
        Assert.All(results, r => Assert.NotNull(r));
    }

    [Fact]
    public async Task SetManyAsync_WithMultipleItems_StoresAll()
    {
        // Arrange
        var cache = CreateCacheService();
        var items = new Dictionary<string, CacheTestUser>
        {
            ["test:user:1"] = new CacheTestUser { Id = 1, Name = "User1" },
            ["test:user:2"] = new CacheTestUser { Id = 2, Name = "User2" },
            ["test:user:3"] = new CacheTestUser { Id = 3, Name = "User3" }
        };

        // Act
        await cache.SetManyAsync(items, TimeSpan.FromMinutes(5));

        // Assert
        var user1 = await cache.GetAsync<CacheTestUser>("test:user:1");
        var user2 = await cache.GetAsync<CacheTestUser>("test:user:2");
        var user3 = await cache.GetAsync<CacheTestUser>("test:user:3");

        Assert.NotNull(user1);
        Assert.NotNull(user2);
        Assert.NotNull(user3);
        Assert.Equal("User1", user1.Name);
        Assert.Equal("User2", user2.Name);
        Assert.Equal("User3", user3.Name);
    }

    [Fact]
    public async Task GetManyAsync_WithMixedExistence_ReturnsOnlyExistingKeys()
    {
        // Arrange
        var cache = CreateCacheService();
        var user1 = new CacheTestUser { Id = 1, Name = "ExistingUser" };
        await cache.SetAsync("test:existing:1", user1);

        var keysToRetrieve = new[] { "test:existing:1", "test:missing:1", "test:missing:2" };

        // Act
        var results = await cache.GetManyAsync<CacheTestUser>(keysToRetrieve);

        // Assert
        Assert.Single(results);
        Assert.True(results.ContainsKey("test:existing:1"));
        Assert.Equal("ExistingUser", results["test:existing:1"].Name);
    }

    [Fact]
    public async Task ClearAsync_RemovesAllEntries()
    {
        // Arrange
        var cache = CreateCacheService();
        var user1 = new CacheTestUser { Id = 1, Name = "User1" };
        var user2 = new CacheTestUser { Id = 2, Name = "User2" };
        await cache.SetAsync("test:user:1", user1);
        await cache.SetAsync("test:user:2", user2);

        // Act
        await cache.ClearAsync();

        // Assert
        Assert.Null(await cache.GetAsync<CacheTestUser>("test:user:1"));
        Assert.Null(await cache.GetAsync<CacheTestUser>("test:user:2"));
    }

    [Fact]
    public async Task SetAsync_WithCustomExpiration_RespectsTimespan()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "test:expiring:1";
        var user = new CacheTestUser { Id = 1, Name = "Temporary" };

        // Act - set with very short expiration
        await cache.SetAsync(key, user, TimeSpan.FromMilliseconds(100));
        var beforeExpiry = await cache.GetAsync<CacheTestUser>(key);

        await Task.Delay(150);
        var afterExpiry = await cache.GetAsync<CacheTestUser>(key);

        // Assert
        Assert.NotNull(beforeExpiry);
        Assert.Null(afterExpiry);
    }

    [Fact]
    public async Task SetAsync_WithDefaultTtl_UsesConfiguredDefault()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "test:default:1";
        var user = new CacheTestUser { Id = 1, Name = "User" };

        // Act - set without explicit expiration
        await cache.SetAsync(key, user);

        // Assert - value should exist (default is 300s)
        var result = await cache.GetAsync<CacheTestUser>(key);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetOrSetAsync_WithCancellation_PropagatesCancellationToken()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "test:cancellation:1";
        var cts = new CancellationTokenSource();
        cts.CancelAfter(10);

        // Act & Assert - TaskCanceledException (subclass of OperationCanceledException) 
        try
        {
            await cache.GetOrSetAsync(
                key,
                async ct =>
                {
                    await Task.Delay(1000, ct);
                    return new CacheTestUser { Id = 1, Name = "User" };
                },
                ct: cts.Token);
            Assert.Fail("Expected cancellation exception to be thrown");
        }
        catch (OperationCanceledException)
        {
            // Expected - includes TaskCanceledException
            Assert.True(cts.Token.IsCancellationRequested);
        }
    }

    [Fact]
    public async Task MultipleTypes_CanCacheDistinctTypes()
    {
        // Arrange
        var cache = CreateCacheService();
        var user = new CacheTestUser { Id = 1, Name = "User" };
        var product = new CacheTestProduct { Sku = "SKU123", Price = 10m };

        // Act
        await cache.SetAsync("test:user:1", user);
        await cache.SetAsync("test:product:1", product);

        var retrievedUser = await cache.GetAsync<CacheTestUser>("test:user:1");
        var retrievedProduct = await cache.GetAsync<CacheTestProduct>("test:product:1");

        // Assert
        Assert.NotNull(retrievedUser);
        Assert.NotNull(retrievedProduct);
        Assert.Equal("User", retrievedUser.Name);
        Assert.Equal("SKU123", retrievedProduct.Sku);
    }

    [Fact]
    public async Task KeyConvention_FollowsPattern()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "myapp:users:profile:42";
        var user = new CacheTestUser { Id = 42, Name = "TestUser" };

        // Act
        await cache.SetAsync(key, user);
        var result = await cache.GetAsync<CacheTestUser>(key);

        // Assert
        Assert.NotNull(result);
    }
}
