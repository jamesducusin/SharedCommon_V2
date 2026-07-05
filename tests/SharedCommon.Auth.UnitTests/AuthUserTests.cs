namespace SharedCommon.Auth.UnitTests;

/// <summary>
/// Behavioral tests for AuthUser model.
/// </summary>
public sealed class AuthUserTests
{
    [Fact]
    public void AuthUser_DefaultInitialization_HasEmptyProperties()
    {
        // Act
        var user = new AuthUser();

        // Assert
        Assert.Equal(string.Empty, user.Id);
        Assert.Equal(string.Empty, user.Email);
        Assert.Empty(user.Roles);
        Assert.Empty(user.Permissions);
        Assert.Empty(user.Claims);
        Assert.False(user.IsAuthenticated);
    }

    [Fact]
    public void AuthUser_WithValidId_IsAuthenticated_ReturnsTrue()
    {
        // Act
        var user = new AuthUser { Id = "user-123" };

        // Assert
        Assert.True(user.IsAuthenticated);
    }

    [Fact]
    public void AuthUser_WithEmptyId_IsAuthenticated_ReturnsFalse()
    {
        // Act
        var user = new AuthUser { Id = string.Empty };

        // Assert
        Assert.False(user.IsAuthenticated);
    }

    [Fact]
    public void AuthUser_WithWhitespaceId_IsAuthenticated_ReturnsFalse()
    {
        // Act
        var user = new AuthUser { Id = "   " };

        // Assert
        Assert.False(user.IsAuthenticated);
    }

    [Fact]
    public void AuthUser_CanBeInitializedWithAllProperties()
    {
        // Arrange
        var id = "user-456";
        var email = "user@example.com";
        var roles = new[] { "admin", "user" };
        var permissions = new[] { "read", "write" };
        var claims = new Dictionary<string, object> { { "custom", "value" } };

        // Act
        var user = new AuthUser
        {
            Id = id,
            Email = email,
            Roles = roles,
            Permissions = permissions,
            Claims = claims
        };

        // Assert
        Assert.Equal(id, user.Id);
        Assert.Equal(email, user.Email);
        Assert.Equal(2, user.Roles.Count());
        Assert.Equal(2, user.Permissions.Count());
        Assert.Single(user.Claims);
        Assert.True(user.IsAuthenticated);
    }

    [Fact]
    public void AuthUser_Roles_IsEnumerable()
    {
        // Arrange
        var roles = new[] { "admin", "user", "moderator" };
        var user = new AuthUser { Roles = roles };

        // Act
        var roleList = user.Roles.ToList();

        // Assert
        Assert.Equal(3, roleList.Count);
        Assert.All(roles, role => Assert.Contains(role, roleList));
    }

    [Fact]
    public void AuthUser_Permissions_IsEnumerable()
    {
        // Arrange
        var permissions = new[] { "read", "write", "delete" };
        var user = new AuthUser { Permissions = permissions };

        // Act
        var permList = user.Permissions.ToList();

        // Assert
        Assert.Equal(3, permList.Count);
        Assert.All(permissions, perm => Assert.Contains(perm, permList));
    }

    [Fact]
    public void AuthUser_Claims_IsDictionary()
    {
        // Arrange
        var claims = new Dictionary<string, object>
        {
            { "claim1", "value1" },
            { "claim2", "value2" },
            { "claim3", 123 }
        };
        var user = new AuthUser { Claims = claims };

        // Act & Assert
        Assert.Equal(3, user.Claims.Count);
        Assert.Equal("value1", user.Claims["claim1"]);
        Assert.Equal("value2", user.Claims["claim2"]);
        Assert.Equal(123, user.Claims["claim3"]);
    }

    [Fact]
    public void AuthUser_WithDifferentIds_NotEqual()
    {
        // Arrange
        var user1 = new AuthUser { Id = "user-123", Email = "user@example.com" };
        var user2 = new AuthUser { Id = "user-456", Email = "user@example.com" };

        // Act & Assert
        Assert.NotEqual(user1, user2);
    }

    [Fact]
    public void AuthUser_MultipleRoles_CanBeQueried()
    {
        // Arrange
        var roles = new[] { "admin", "user", "moderator", "viewer" };
        var user = new AuthUser { Roles = roles };

        // Act
        var hasAdminRole = user.Roles.Contains("admin");
        var hasUserRole = user.Roles.Contains("user");
        var hasUnknownRole = user.Roles.Contains("superadmin");

        // Assert
        Assert.True(hasAdminRole);
        Assert.True(hasUserRole);
        Assert.False(hasUnknownRole);
    }

    [Fact]
    public void AuthUser_MultiplePermissions_CanBeQueried()
    {
        // Arrange
        var permissions = new[] { "read", "write", "delete", "approve" };
        var user = new AuthUser { Permissions = permissions };

        // Act
        var canRead = user.Permissions.Contains("read");
        var canWrite = user.Permissions.Contains("write");
        var canExecute = user.Permissions.Contains("execute");

        // Assert
        Assert.True(canRead);
        Assert.True(canWrite);
        Assert.False(canExecute);
    }

    [Fact]
    public void AuthUser_ClaimsCanContainMultipleTypes()
    {
        // Arrange
        var claims = new Dictionary<string, object>
        {
            { "stringValue", "text" },
            { "intValue", 42 },
            { "boolValue", true },
            { "doubleValue", 3.14 }
        };
        var user = new AuthUser { Claims = claims };

        // Act & Assert
        Assert.Equal("text", user.Claims["stringValue"]);
        Assert.Equal(42, user.Claims["intValue"]);
        Assert.Equal(true, user.Claims["boolValue"]);
        Assert.Equal(3.14, user.Claims["doubleValue"]);
    }

    [Fact]
    public void AuthUser_DefaultRoles_IsEmptyList()
    {
        // Arrange & Act
        var user = new AuthUser();

        // Assert
        var rolesList = user.Roles as ICollection<string>;
        Assert.NotNull(rolesList);
        Assert.Empty(rolesList);
    }

    [Fact]
    public void AuthUser_DefaultPermissions_IsEmptyList()
    {
        // Arrange & Act
        var user = new AuthUser();

        // Assert
        var permsList = user.Permissions as ICollection<string>;
        Assert.NotNull(permsList);
        Assert.Empty(permsList);
    }

    [Fact]
    public void AuthUser_DefaultClaims_IsEmptyDictionary()
    {
        // Arrange & Act
        var user = new AuthUser();

        // Assert
        Assert.Empty(user.Claims);
    }
}
