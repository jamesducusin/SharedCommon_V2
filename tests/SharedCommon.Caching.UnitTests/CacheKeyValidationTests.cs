using Microsoft.Extensions.Caching.Memory;

namespace SharedCommon.Caching.UnitTests;

/// <summary>
/// Test fixtures for key validation.
/// </summary>
internal sealed class KeyTestUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Behavioral tests for cache key validation and conventions.
/// Tests follow the {package}:{entity}:{id} key pattern.
/// </summary>
public sealed class CacheKeyValidationTests
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
    public async Task KeyConvention_WithStandardPattern_WorksCorrectly()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "users:profile:42";
        var user = new KeyTestUser { Id = 42, Name = "TestUser" };

        // Act
        await cache.SetAsync(key, user);
        var result = await cache.GetAsync<KeyTestUser>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestUser", result.Name);
    }

    [Fact]
    public async Task KeyConvention_WithPackagePrefix_WorksCorrectly()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "myapp:users:profile:123";
        var user = new KeyTestUser { Id = 123, Name = "User123" };

        // Act
        await cache.SetAsync(key, user);
        var result = await cache.GetAsync<KeyTestUser>(key);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task KeyConvention_WithNumbers_WorksCorrectly()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "products:sku:999888777";
        var user = new KeyTestUser { Id = 999888777, Name = "NumericId" };

        // Act
        await cache.SetAsync(key, user);
        var result = await cache.GetAsync<KeyTestUser>(key);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task KeyConvention_WithHyphens_WorksCorrectly()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "my-app:my-entity:my-id-42";
        var user = new KeyTestUser { Id = 1, Name = "Hyphenated" };

        // Act
        await cache.SetAsync(key, user);
        var result = await cache.GetAsync<KeyTestUser>(key);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task KeyConvention_WithUnderscores_WorksCorrectly()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "my_app:my_entity:my_id_42";
        var user = new KeyTestUser { Id = 1, Name = "Underscored" };

        // Act
        await cache.SetAsync(key, user);
        var result = await cache.GetAsync<KeyTestUser>(key);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task KeyConvention_WithGUID_WorksCorrectly()
    {
        // Arrange
        var cache = CreateCacheService();
        var guid = Guid.NewGuid().ToString();
        var key = $"users:profile:{guid}";
        var user = new KeyTestUser { Id = 1, Name = "GUIDKey" };

        // Act
        await cache.SetAsync(key, user);
        var result = await cache.GetAsync<KeyTestUser>(key);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task KeyConvention_CaseSensitive_DifferentKeysForDifferentCase()
    {
        // Arrange
        var cache = CreateCacheService();
        var keyLower = "users:profile:1";
        var keyUpper = "USERS:PROFILE:1";
        var user = new KeyTestUser { Id = 1, Name = "User" };

        // Act
        await cache.SetAsync(keyLower, user);
        var resultLower = await cache.GetAsync<KeyTestUser>(keyLower);
        var resultUpper = await cache.GetAsync<KeyTestUser>(keyUpper);

        // Assert
        Assert.NotNull(resultLower);
        Assert.Null(resultUpper);
    }

    [Fact]
    public async Task KeyConvention_Exact_MatchRequiredForRetrieval()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "exact:match:key";
        var user = new KeyTestUser { Id = 1, Name = "Exact" };

        // Act
        await cache.SetAsync(key, user);
        var result1 = await cache.GetAsync<KeyTestUser>(key);
        var result2 = await cache.GetAsync<KeyTestUser>("exact:match:key");
        var result3 = await cache.GetAsync<KeyTestUser>("exact:match:key:extra");

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Null(result3);
    }

    [Fact]
    public async Task KeyConvention_Remove_ExactMatchRequired()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "remove:test:key";
        var user = new KeyTestUser { Id = 1, Name = "ToRemove" };
        await cache.SetAsync(key, user);

        // Act
        await cache.RemoveAsync(key);
        var result = await cache.GetAsync<KeyTestUser>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task KeyConvention_Remove_PartialKeyDoesNothing()
    {
        // Arrange
        var cache = CreateCacheService();
        var fullKey = "remove:test:full:key";
        var user = new KeyTestUser { Id = 1, Name = "ToRemove" };
        await cache.SetAsync(fullKey, user);

        // Act
        await cache.RemoveAsync("remove:test");
        var result = await cache.GetAsync<KeyTestUser>(fullKey);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task KeyConvention_MultipleTypes_IndependentByKey()
    {
        // Arrange
        var cache = CreateCacheService();
        var userKey = "users:profile:1";
        var productKey = "products:detail:1";
        var user = new KeyTestUser { Id = 1, Name = "User" };

        // Act
        await cache.SetAsync(userKey, user);
        var userResult = await cache.GetAsync<KeyTestUser>(userKey);
        var productResult = await cache.GetAsync<KeyTestUser>(productKey);

        // Assert
        Assert.NotNull(userResult);
        Assert.Null(productResult);
    }

    [Fact]
    public async Task KeyConvention_LongKey_Supported()
    {
        // Arrange
        var cache = CreateCacheService();
        var longKey = "verylongpackagename:verylongentityname:verylongidvalue:12345";
        var user = new KeyTestUser { Id = 1, Name = "LongKey" };

        // Act
        await cache.SetAsync(longKey, user);
        var result = await cache.GetAsync<KeyTestUser>(longKey);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task KeyConvention_WithColons_Parsed()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "app:v1:users:profile:usr-12345:metadata";
        var user = new KeyTestUser { Id = 1, Name = "MultiColon" };

        // Act
        await cache.SetAsync(key, user);
        var result = await cache.GetAsync<KeyTestUser>(key);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task KeyConvention_Consistency_SameKeyRetrievesSameValue()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "consistency:test:key";
        var user = new KeyTestUser { Id = 42, Name = "Consistent" };

        // Act
        await cache.SetAsync(key, user);
        var result1 = await cache.GetAsync<KeyTestUser>(key);
        var result2 = await cache.GetAsync<KeyTestUser>(key);
        var result3 = await cache.GetAsync<KeyTestUser>(key);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.Equal(result1.Id, result2.Id);
        Assert.Equal(result2.Id, result3.Id);
    }

    [Fact]
    public async Task KeyConvention_WildcardPatterns_Unsupported()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "users:profile:*";
        var user = new KeyTestUser { Id = 1, Name = "Wildcard" };

        // Act - wildcard is treated as literal key
        await cache.SetAsync(key, user);
        var result1 = await cache.GetAsync<KeyTestUser>(key);
        var result2 = await cache.GetAsync<KeyTestUser>("users:profile:1");

        // Assert
        Assert.NotNull(result1);
        Assert.Null(result2);
    }

    [Fact]
    public async Task KeyConvention_SpecialCharacters_InKey()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "app:users:profile:user@example.com";
        var user = new KeyTestUser { Id = 1, Name = "SpecialChar" };

        // Act
        await cache.SetAsync(key, user);
        var result = await cache.GetAsync<KeyTestUser>(key);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task KeyConvention_NumericOnlySegments()
    {
        // Arrange
        var cache = CreateCacheService();
        var key = "123:456:789";
        var user = new KeyTestUser { Id = 789, Name = "NumericKey" };

        // Act
        await cache.SetAsync(key, user);
        var result = await cache.GetAsync<KeyTestUser>(key);

        // Assert
        Assert.NotNull(result);
    }
}
