using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace SharedCommon.Auth.UnitTests;

/// <summary>
/// Behavioral tests for ICurrentUser context and claim extraction.
/// </summary>
public sealed class CurrentUserTests
{
    private static CurrentUser CreateCurrentUser(ClaimsPrincipal? principal = null)
    {
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        if (principal != null)
        {
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(ctx => ctx.User).Returns(principal);
            mockHttpContextAccessor.Setup(accessor => accessor.HttpContext).Returns(mockHttpContext.Object);
        }
        else
        {
            mockHttpContextAccessor.Setup(accessor => accessor.HttpContext).Returns((HttpContext?)null);
        }

        return new CurrentUser(mockHttpContextAccessor.Object);
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipal(
        string userId = "user-123",
        string email = "user@example.com",
        string[]? roles = null,
        string[]? permissions = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email)
        };

        if (roles != null)
        {
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        }

        if (permissions != null)
        {
            claims.AddRange(permissions.Select(p => new Claim("permission", p)));
        }

        var identity = new ClaimsIdentity(claims, "test");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public void IsAuthenticated_WithAuthenticatedPrincipal_ReturnsTrue()
    {
        // Arrange
        var principal = CreateAuthenticatedPrincipal();
        var currentUser = CreateCurrentUser(principal);

        // Act
        var isAuthenticated = currentUser.IsAuthenticated;

        // Assert
        Assert.True(isAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_WithNoPrincipal_ReturnsFalse()
    {
        // Arrange
        var currentUser = CreateCurrentUser(null);

        // Act
        var isAuthenticated = currentUser.IsAuthenticated;

        // Assert
        Assert.False(isAuthenticated);
    }

    [Fact]
    public void UserId_WithAuthenticatedPrincipal_ReturnsUserId()
    {
        // Arrange
        var userId = "user-456";
        var principal = CreateAuthenticatedPrincipal(userId: userId);
        var currentUser = CreateCurrentUser(principal);

        // Act
        var result = currentUser.UserId;

        // Assert
        Assert.Equal(userId, result);
    }

    [Fact]
    public void UserId_WithNoPrincipal_ReturnsNull()
    {
        // Arrange
        var currentUser = CreateCurrentUser(null);

        // Act
        var result = currentUser.UserId;

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Email_WithAuthenticatedPrincipal_ReturnsEmail()
    {
        // Arrange
        var email = "john@example.com";
        var principal = CreateAuthenticatedPrincipal(email: email);
        var currentUser = CreateCurrentUser(principal);

        // Act
        var result = currentUser.Email;

        // Assert
        Assert.Equal(email, result);
    }

    [Fact]
    public void Email_WithNoPrincipal_ReturnsNull()
    {
        // Arrange
        var currentUser = CreateCurrentUser(null);

        // Act
        var result = currentUser.Email;

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Roles_WithAuthenticatedPrincipal_ReturnsAllRoles()
    {
        // Arrange
        var roles = new[] { "admin", "user", "moderator" };
        var principal = CreateAuthenticatedPrincipal(roles: roles);
        var currentUser = CreateCurrentUser(principal);

        // Act
        var result = currentUser.Roles.ToList();

        // Assert
        Assert.Equal(roles.Length, result.Count);
        Assert.All(roles, role => Assert.Contains(role, result));
    }

    [Fact]
    public void Roles_WithNoPrincipal_ReturnsEmptyCollection()
    {
        // Arrange
        var currentUser = CreateCurrentUser(null);

        // Act
        var result = currentUser.Roles.ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Roles_WithNoRoleClaims_ReturnsEmptyCollection()
    {
        // Arrange
        var principal = CreateAuthenticatedPrincipal(roles: Array.Empty<string>());
        var currentUser = CreateCurrentUser(principal);

        // Act
        var result = currentUser.Roles.ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Permissions_WithAuthenticatedPrincipal_ReturnsAllPermissions()
    {
        // Arrange
        var permissions = new[] { "read", "write", "delete" };
        var principal = CreateAuthenticatedPrincipal(permissions: permissions);
        var currentUser = CreateCurrentUser(principal);

        // Act
        var result = currentUser.Permissions.ToList();

        // Assert
        Assert.Equal(permissions.Length, result.Count);
        Assert.All(permissions, permission => Assert.Contains(permission, result));
    }

    [Fact]
    public void Permissions_WithNoPrincipal_ReturnsEmptyCollection()
    {
        // Arrange
        var currentUser = CreateCurrentUser(null);

        // Act
        var result = currentUser.Permissions.ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Permissions_WithNoPermissionClaims_ReturnsEmptyCollection()
    {
        // Arrange
        var principal = CreateAuthenticatedPrincipal(permissions: Array.Empty<string>());
        var currentUser = CreateCurrentUser(principal);

        // Act
        var result = currentUser.Permissions.ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void User_ReturnsAuthUserWithCorrectProperties()
    {
        // Arrange
        var userId = "user-789";
        var email = "jane@example.com";
        var roles = new[] { "admin" };
        var permissions = new[] { "read" };
        var principal = CreateAuthenticatedPrincipal(userId, email, roles, permissions);
        var currentUser = CreateCurrentUser(principal);

        // Act
        var user = currentUser.User;

        // Assert
        Assert.Equal(userId, user.Id);
        Assert.Equal(email, user.Email);
        Assert.Single(user.Roles);
        Assert.Contains("admin", user.Roles);
        Assert.Single(user.Permissions);
    }

    [Fact]
    public void User_WithNoPrincipal_ReturnsAuthUserWithEmptyProperties()
    {
        // Arrange
        var currentUser = CreateCurrentUser(null);

        // Act
        var user = currentUser.User;

        // Assert
        Assert.Equal(string.Empty, user.Id);
        Assert.Equal(string.Empty, user.Email);
        Assert.Empty(user.Roles);
        Assert.Empty(user.Permissions);
        Assert.False(user.IsAuthenticated);
    }

    [Fact]
    public void User_IsAuthenticated_ReflectsPrincipalAuthentication()
    {
        // Arrange
        var principal = CreateAuthenticatedPrincipal();
        var currentUser = CreateCurrentUser(principal);

        // Act
        var user = currentUser.User;

        // Assert
        Assert.True(user.IsAuthenticated);
    }

    [Fact]
    public void User_Claims_ContainsAllClaims()
    {
        // Arrange
        var principal = CreateAuthenticatedPrincipal(
            userId: "user-001",
            email: "test@example.com",
            roles: new[] { "admin" },
            permissions: new[] { "read" });
        var currentUser = CreateCurrentUser(principal);

        // Act
        var user = currentUser.User;

        // Assert
        Assert.NotEmpty(user.Claims);
        Assert.Contains(ClaimTypes.NameIdentifier, user.Claims.Keys);
        Assert.Contains(ClaimTypes.Email, user.Claims.Keys);
        Assert.Contains(ClaimTypes.Role, user.Claims.Keys);
        Assert.Contains("permission", user.Claims.Keys);
    }

    [Fact]
    public void UserId_PrefersNameIdentifierOverSubClaim()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "nameidentifier-value"),
            new("sub", "sub-value"),
            new(ClaimTypes.Email, "user@example.com")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        var currentUser = CreateCurrentUser(principal);

        // Act
        var result = currentUser.UserId;

        // Assert
        Assert.Equal("nameidentifier-value", result);
    }

    [Fact]
    public void UserId_FallsBackToSubClaim_WhenNameIdentifierNotPresent()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("sub", "sub-value"),
            new(ClaimTypes.Email, "user@example.com")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        var currentUser = CreateCurrentUser(principal);

        // Act
        var result = currentUser.UserId;

        // Assert
        Assert.Equal("sub-value", result);
    }

    [Fact]
    public void MultipleRolesAndPermissions_AllExtractedCorrectly()
    {
        // Arrange
        var roles = new[] { "admin", "user", "moderator", "viewer" };
        var permissions = new[] { "read", "write", "delete", "approve", "audit" };
        var principal = CreateAuthenticatedPrincipal(
            roles: roles,
            permissions: permissions);
        var currentUser = CreateCurrentUser(principal);

        // Act
        var extractedRoles = currentUser.Roles.ToList();
        var extractedPermissions = currentUser.Permissions.ToList();

        // Assert
        Assert.Equal(4, extractedRoles.Count);
        Assert.Equal(5, extractedPermissions.Count);
        Assert.All(roles, role => Assert.Contains(role, extractedRoles));
        Assert.All(permissions, perm => Assert.Contains(perm, extractedPermissions));
    }

    [Fact]
    public void NullHttpContextAccessor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CurrentUser(null!));
    }
}
