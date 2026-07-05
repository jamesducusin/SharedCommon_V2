namespace SharedCommon.Caching.UnitTests;

/// <summary>
/// Tests for CachingOptions configuration validation and behavior.
/// </summary>
public sealed class CacheOptionsTests
{
    [Fact]
    public void CachingOptions_DefaultInitialization_HasExpectedDefaults()
    {
        // Act
        var options = new CachingOptions();

        // Assert
        Assert.Equal("Hybrid", options.DefaultProvider);
        Assert.Equal(300, options.DefaultTtlSeconds);
        Assert.NotNull(options.Memory);
        Assert.NotNull(options.Redis);
        Assert.NotNull(options.Database);
    }

    [Fact]
    public void MemoryCacheOptions_DefaultInitialization()
    {
        // Act
        var options = new MemoryCacheOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(10_000, options.MaximumSize);
        Assert.Equal(300, options.SlidingExpiration);
    }

    [Fact]
    public void MemoryCacheOptions_WithCustomMaximumSize_StoresValue()
    {
        // Act
        var options = new MemoryCacheOptions { MaximumSize = 5000 };

        // Assert
        Assert.Equal(5000, options.MaximumSize);
    }

    [Fact]
    public void MemoryCacheOptions_WithSlidingExpiration_Configured()
    {
        // Act
        var options = new MemoryCacheOptions { SlidingExpiration = 600 };

        // Assert
        Assert.Equal(600, options.SlidingExpiration);
    }

    [Fact]
    public void MemoryCacheOptions_WithAbsoluteExpiration_Configured()
    {
        // Act
        var options = new MemoryCacheOptions { AbsoluteExpiration = 3600 };

        // Assert
        Assert.Equal(3600, options.AbsoluteExpiration);
    }

    [Fact]
    public void RedisCacheOptions_DefaultInitialization()
    {
        // Act
        var options = new RedisCacheOptions();

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal("sharedcommon:", options.KeyPrefix);
        Assert.Equal(300, options.DefaultTtlSeconds);
        Assert.Equal(0, options.DatabaseId);
        Assert.False(options.Ssl);
    }

    [Fact]
    public void RedisCacheOptions_WithConnectionString_ParsesSuccessfully()
    {
        // Act
        var options = new RedisCacheOptions
        {
            Enabled = true,
            Connection = "localhost:6379,allowAdmin=true",
            KeyPrefix = "myapp:"
        };

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal("localhost:6379,allowAdmin=true", options.Connection);
        Assert.Equal("myapp:", options.KeyPrefix);
    }

    [Fact]
    public void RedisCacheOptions_WithSslEnabled_ConfiguresSecurely()
    {
        // Act
        var options = new RedisCacheOptions
        {
            Enabled = true,
            Ssl = true,
            Connection = "redis.azure.com:6380"
        };

        // Assert
        Assert.True(options.Ssl);
        Assert.True(options.Enabled);
        Assert.Equal("redis.azure.com:6380", options.Connection);
    }

    [Fact]
    public void RedisCacheOptions_WithCustomDatabaseId_StoresValue()
    {
        // Act
        var options = new RedisCacheOptions { DatabaseId = 5 };

        // Assert
        Assert.Equal(5, options.DatabaseId);
    }

    [Fact]
    public void RedisCacheOptions_WithTimeouts_Configured()
    {
        // Act
        var options = new RedisCacheOptions
        {
            ConnectTimeout = 10_000,
            SyncTimeout = 5_000
        };

        // Assert
        Assert.Equal(10_000, options.ConnectTimeout);
        Assert.Equal(5_000, options.SyncTimeout);
    }

    [Fact]
    public void DatabaseCacheOptions_DefaultInitialization()
    {
        // Act
        var options = new DatabaseCacheOptions();

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal("CacheItems", options.TableName);
        Assert.Equal(300, options.DefaultTtlSeconds);
        Assert.Equal(3600, options.CleanupIntervalSeconds);
    }

    [Fact]
    public void DatabaseCacheOptions_WithConnectionString_Configured()
    {
        // Act
        var options = new DatabaseCacheOptions
        {
            Enabled = true,
            ConnectionString = "Server=localhost;Database=cache",
            TableName = "CustomCache"
        };

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal("Server=localhost;Database=cache", options.ConnectionString);
        Assert.Equal("CustomCache", options.TableName);
    }

    [Fact]
    public void CachingOptions_Provider_CanBeChangedAtRuntime()
    {
        // Arrange
        var options = new CachingOptions { DefaultProvider = "Memory" };

        // Act
        options.DefaultProvider = "Redis";

        // Assert
        Assert.Equal("Redis", options.DefaultProvider);
    }

    [Fact]
    public void CachingOptions_MultiTierConfiguration_AllPropertiesAccessible()
    {
        // Act
        var options = new CachingOptions
        {
            DefaultProvider = "Hybrid",
            DefaultTtlSeconds = 600,
            SerializationFormat = "Json",
            Memory = new MemoryCacheOptions { MaximumSize = 5000, SlidingExpiration = 400 },
            Redis = new RedisCacheOptions
            {
                Enabled = true,
                Connection = "localhost:6379",
                KeyPrefix = "app:"
            },
            Database = new DatabaseCacheOptions
            {
                Enabled = false,
                ConnectionString = "Server=db",
                TableName = "CacheData"
            }
        };

        // Assert
        Assert.Equal("Hybrid", options.DefaultProvider);
        Assert.Equal(600, options.DefaultTtlSeconds);
        Assert.Equal("Json", options.SerializationFormat);
        Assert.Equal(5000, options.Memory.MaximumSize);
        Assert.True(options.Redis.Enabled);
        Assert.Equal("app:", options.Redis.KeyPrefix);
        Assert.False(options.Database.Enabled);
        Assert.Equal("CacheData", options.Database.TableName);
    }
}
