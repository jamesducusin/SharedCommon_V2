using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SharedCommon.Auth.UnitTests;

/// <summary>
/// Behavioral tests for JWT token generation, validation, and revocation workflows.
/// </summary>
public sealed class JwtAuthServiceTests
{
    private const string ValidSecretKey = "this-is-a-valid-secret-key-minimum-32-chars-xxxxxxx";
    private const string ValidIssuer = "https://auth.example.com";
    private const string ValidAudience = "https://api.example.com";

    private static JwtAuthService CreateService(
        string? secretKey = null,
        string? issuer = null,
        string? audience = null,
        Action<JwtOptions>? jwtCustomizer = null,
        Action<JwtValidationOptions>? validationCustomizer = null)
    {
        var jwtOptions = new JwtOptions
        {
            SecretKey = secretKey ?? ValidSecretKey,
            Issuer = issuer ?? ValidIssuer,
            Audience = audience ?? ValidAudience,
            ExpirationMinutes = 60,
            Enabled = true
        };

        jwtCustomizer?.Invoke(jwtOptions);

        var validationOptions = new JwtValidationOptions
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ClockSkewSeconds = 0
        };

        validationCustomizer?.Invoke(validationOptions);
        jwtOptions.Validation = validationOptions;

        var options = new AuthOptions { Jwt = jwtOptions };
        var mockLogger = new Mock<ILogger<JwtAuthService>>();
        return new JwtAuthService(Options.Create(options), mockLogger.Object);
    }

    [Fact]
    public async Task GenerateTokenAsync_WithValidInputs_ReturnsTokenSuccess()
    {
        // Arrange
        var service = CreateService();
        var userId = "user-123";
        var email = "user@example.com";
        var roles = new[] { "admin", "user" };

        // Act
        var result = await service.GenerateTokenAsync(userId, email, roles);

        // Assert
        Assert.IsType<Result<string>.Success>(result);
        var tokenResult = (Result<string>.Success)result;
        Assert.False(string.IsNullOrWhiteSpace(tokenResult.Data));
    }

    [Fact]
    public async Task GenerateTokenAsync_WithNullUserId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GenerateTokenAsync(null!, "user@example.com", []);

        // Assert
        Assert.IsType<Result<string>.Failure>(result);
        var failureResult = (Result<string>.Failure)result;
        Assert.Equal("INVALID_USER_ID", failureResult.Code);
    }

    [Fact]
    public async Task GenerateTokenAsync_WithEmptyUserId_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GenerateTokenAsync(string.Empty, "user@example.com", []);

        // Assert
        Assert.IsType<Result<string>.Failure>(result);
        var failureResult = (Result<string>.Failure)result;
        Assert.Equal("INVALID_USER_ID", failureResult.Code);
    }

    [Fact]
    public async Task GenerateTokenAsync_TokenContainsSubClaim()
    {
        // Arrange
        var service = CreateService();
        var userId = "user-123";
        var email = "user@example.com";

        // Act
        var result = await service.GenerateTokenAsync(userId, email, []);

        // Assert
        Assert.IsType<Result<string>.Success>(result);
        var tokenResult = (Result<string>.Success)result;
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenResult.Data);

        Assert.Equal(userId, token.Subject);
    }

    [Fact]
    public async Task GenerateTokenAsync_WithIncludeEmailTrue_TokenContainsEmailClaim()
    {
        // Arrange
        var service = CreateService(jwtCustomizer: opts =>
        {
            opts.Claims.IncludeEmail = true;
        });
        var userId = "user-123";
        var email = "user@example.com";

        // Act
        var result = await service.GenerateTokenAsync(userId, email, []);

        // Assert
        Assert.IsType<Result<string>.Success>(result);
        var tokenResult = (Result<string>.Success)result;
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenResult.Data);

        var emailClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        Assert.NotNull(emailClaim);
        Assert.Equal(email, emailClaim.Value);
    }

    [Fact]
    public async Task GenerateTokenAsync_WithIncludeEmailFalse_TokenDoesNotContainEmailClaim()
    {
        // Arrange
        var service = CreateService(jwtCustomizer: opts =>
        {
            opts.Claims.IncludeEmail = false;
        });
        var userId = "user-123";
        var email = "user@example.com";

        // Act
        var result = await service.GenerateTokenAsync(userId, email, []);

        // Assert
        Assert.IsType<Result<string>.Success>(result);
        var tokenResult = (Result<string>.Success)result;
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenResult.Data);

        var emailClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        Assert.Null(emailClaim);
    }

    [Fact]
    public async Task GenerateTokenAsync_WithIncludeRolesTrue_TokenContainsRoleClaims()
    {
        // Arrange
        var service = CreateService(jwtCustomizer: opts =>
        {
            opts.Claims.IncludeRoles = true;
        });
        var userId = "user-123";
        var roles = new[] { "admin", "user", "moderator" };

        // Act
        var result = await service.GenerateTokenAsync(userId, "user@example.com", roles);

        // Assert
        Assert.IsType<Result<string>.Success>(result);
        var tokenResult = (Result<string>.Success)result;
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenResult.Data);

        var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        Assert.Equal(roles.Length, roleClaims.Count);
        Assert.All(roles, role => Assert.Contains(roleClaims, rc => rc.Value == role));
    }

    [Fact]
    public async Task GenerateTokenAsync_WithIncludeRolesFalse_TokenDoesNotContainRoleClaims()
    {
        // Arrange
        var service = CreateService(jwtCustomizer: opts =>
        {
            opts.Claims.IncludeRoles = false;
        });
        var userId = "user-123";
        var roles = new[] { "admin", "user" };

        // Act
        var result = await service.GenerateTokenAsync(userId, "user@example.com", roles);

        // Assert
        Assert.IsType<Result<string>.Success>(result);
        var tokenResult = (Result<string>.Success)result;
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenResult.Data);

        var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        Assert.Empty(roleClaims);
    }

    [Fact]
    public async Task GenerateTokenAsync_TokenContainsJtiClaim()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GenerateTokenAsync("user-123", "user@example.com", []);

        // Assert
        Assert.IsType<Result<string>.Success>(result);
        var tokenResult = (Result<string>.Success)result;
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenResult.Data);

        Assert.NotNull(token.Id);
        Assert.False(string.IsNullOrWhiteSpace(token.Id));
    }

    [Fact]
    public async Task GenerateTokenAsync_WithCustomExpiration_TokenExpiresAtSpecifiedTime()
    {
        // Arrange
        var service = CreateService();
        var customExpiration = TimeSpan.FromMinutes(30);

        // Act
        var result = await service.GenerateTokenAsync("user-123", "user@example.com", [], customExpiration);

        // Assert
        Assert.IsType<Result<string>.Success>(result);
        var tokenResult = (Result<string>.Success)result;
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenResult.Data);

        var expectedExpiry = DateTime.UtcNow.Add(customExpiration);
        Assert.True((token.ValidTo - expectedExpiry).Duration().TotalSeconds < 5); // Within 5 seconds
    }

    [Fact]
    public async Task GenerateTokenAsync_WithDefaultExpiration_UsesExpirationMinutesFromConfig()
    {
        // Arrange
        var service = CreateService(jwtCustomizer: opts => opts.ExpirationMinutes = 45);

        // Act
        var result = await service.GenerateTokenAsync("user-123", "user@example.com", []);

        // Assert
        Assert.IsType<Result<string>.Success>(result);
        var tokenResult = (Result<string>.Success)result;
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenResult.Data);

        var expectedExpiry = DateTime.UtcNow.AddMinutes(45);
        Assert.True((token.ValidTo - expectedExpiry).Duration().TotalSeconds < 5);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        var service = CreateService();
        var userId = "user-123";
        var email = "user@example.com";
        var generateResult = await service.GenerateTokenAsync(userId, email, []);
        var tokenResult = (Result<string>.Success)generateResult;

        // Act
        var result = await service.ValidateTokenAsync(tokenResult.Data);

        // Assert
        Assert.IsType<Result<IAuthUser>.Success>(result);
        var validationResult = (Result<IAuthUser>.Success)result;
        Assert.Equal(userId, validationResult.Data.Id);
        Assert.Equal(email, validationResult.Data.Email);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithEmptyToken_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ValidateTokenAsync(string.Empty);

        // Assert
        Assert.IsType<Result<IAuthUser>.Failure>(result);
        var failureResult = (Result<IAuthUser>.Failure)result;
        Assert.Equal("TOKEN_INVALID", failureResult.Code);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithNullToken_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ValidateTokenAsync(null!);

        // Assert
        Assert.IsType<Result<IAuthUser>.Failure>(result);
        var failureResult = (Result<IAuthUser>.Failure)result;
        Assert.Equal("TOKEN_INVALID", failureResult.Code);
    }



    [Fact]
    public async Task ValidateTokenAsync_WithWrongIssuer_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var generateResult = await service.GenerateTokenAsync("user-123", "user@example.com", []);
        var tokenResult = (Result<string>.Success)generateResult;

        var serviceWithDifferentIssuer = CreateService(issuer: "https://wrong-issuer.com");

        // Act
        var result = await serviceWithDifferentIssuer.ValidateTokenAsync(tokenResult.Data);

        // Assert
        Assert.IsType<Result<IAuthUser>.Failure>(result);
        var failureResult = (Result<IAuthUser>.Failure)result;
        Assert.Equal("TOKEN_INVALID", failureResult.Code);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithWrongAudience_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var generateResult = await service.GenerateTokenAsync("user-123", "user@example.com", []);
        var tokenResult = (Result<string>.Success)generateResult;

        var serviceWithDifferentAudience = CreateService(audience: "https://wrong-audience.com");

        // Act
        var result = await serviceWithDifferentAudience.ValidateTokenAsync(tokenResult.Data);

        // Assert
        Assert.IsType<Result<IAuthUser>.Failure>(result);
        var failureResult = (Result<IAuthUser>.Failure)result;
        Assert.Equal("TOKEN_INVALID", failureResult.Code);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithWrongSecret_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var generateResult = await service.GenerateTokenAsync("user-123", "user@example.com", []);
        var tokenResult = (Result<string>.Success)generateResult;

        var serviceWithWrongSecret = CreateService(secretKey: "this-is-a-different-secret-key-minimum-32-chars-xx");

        // Act
        var result = await serviceWithWrongSecret.ValidateTokenAsync(tokenResult.Data);

        // Assert
        Assert.IsType<Result<IAuthUser>.Failure>(result);
        var failureResult = (Result<IAuthUser>.Failure)result;
        Assert.Equal("TOKEN_INVALID", failureResult.Code);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithExpiredToken_ReturnsExpiredFailure()
    {
        // Arrange
        var service = CreateService();
        var generateResult = await service.GenerateTokenAsync(
            "user-123",
            "user@example.com",
            [],
            TimeSpan.FromSeconds(1)); // Expire in 1 second
        var tokenResult = (Result<string>.Success)generateResult;

        // Wait for token to expire
        await Task.Delay(1100);

        // Act
        var result = await service.ValidateTokenAsync(tokenResult.Data);

        // Assert
        Assert.IsType<Result<IAuthUser>.Failure>(result);
        var failureResult = (Result<IAuthUser>.Failure)result;
        Assert.Equal("TOKEN_EXPIRED", failureResult.Code);
    }

    [Fact]
    public async Task RevokeTokenAsync_WithValidToken_SuccessfullyRevokes()
    {
        // Arrange
        var service = CreateService();
        var generateResult = await service.GenerateTokenAsync("user-123", "user@example.com", []);
        var tokenResult = (Result<string>.Success)generateResult;

        // Act
        var revokeResult = await service.RevokeTokenAsync(tokenResult.Data);

        // Assert
        Assert.IsType<Result.Success>(revokeResult);
    }

    [Fact]
    public async Task ValidateTokenAsync_AfterRevocation_ReturnsRevokedFailure()
    {
        // Arrange
        var service = CreateService();
        var generateResult = await service.GenerateTokenAsync("user-123", "user@example.com", []);
        var tokenResult = (Result<string>.Success)generateResult;

        // Revoke the token
        await service.RevokeTokenAsync(tokenResult.Data);

        // Act
        var validationResult = await service.ValidateTokenAsync(tokenResult.Data);

        // Assert
        Assert.IsType<Result<IAuthUser>.Failure>(validationResult);
        var failureResult = (Result<IAuthUser>.Failure)validationResult;
        Assert.Equal("TOKEN_REVOKED", failureResult.Code);
    }

    [Fact]
    public async Task RevokeTokenAsync_WithNullToken_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.RevokeTokenAsync(null!);

        // Assert
        Assert.IsType<Result.Failure>(result);
        var failureResult = (Result.Failure)result;
        Assert.Equal("TOKEN_INVALID", failureResult.Code);
    }

    [Fact]
    public async Task RevokeTokenAsync_WithEmptyToken_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.RevokeTokenAsync(string.Empty);

        // Assert
        Assert.IsType<Result.Failure>(result);
        var failureResult = (Result.Failure)result;
        Assert.Equal("TOKEN_INVALID", failureResult.Code);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_GeneratesNewToken()
    {
        // Arrange
        var service = CreateService();
        var generateResult = await service.GenerateTokenAsync("user-123", "user@example.com", ["admin"]);
        var tokenResult = (Result<string>.Success)generateResult;

        // Act
        var refreshResult = await service.RefreshTokenAsync(tokenResult.Data);

        // Assert
        Assert.IsType<Result<string>.Success>(refreshResult);
        var newTokenResult = (Result<string>.Success)refreshResult;
        Assert.False(string.IsNullOrWhiteSpace(newTokenResult.Data));
        Assert.NotEqual(tokenResult.Data, newTokenResult.Data); // Different tokens (different jti)
    }

    [Fact]
    public async Task RefreshTokenAsync_GeneratesTokenWithSameUserId()
    {
        // Arrange
        var service = CreateService();
        var userId = "user-123";
        var generateResult = await service.GenerateTokenAsync(userId, "user@example.com", []);
        var tokenResult = (Result<string>.Success)generateResult;

        // Act
        var refreshResult = await service.RefreshTokenAsync(tokenResult.Data);

        // Assert
        Assert.IsType<Result<string>.Success>(refreshResult);
        var newTokenResult = (Result<string>.Success)refreshResult;
        var handler = new JwtSecurityTokenHandler();
        var newToken = handler.ReadJwtToken(newTokenResult.Data);
        Assert.Equal(userId, newToken.Subject);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithNullToken_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.RefreshTokenAsync(null!);

        // Assert
        Assert.IsType<Result<string>.Failure>(result);
        var failureResult = (Result<string>.Failure)result;
        Assert.Equal("TOKEN_INVALID", failureResult.Code);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithEmptyToken_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.RefreshTokenAsync(string.Empty);

        // Assert
        Assert.IsType<Result<string>.Failure>(result);
        var failureResult = (Result<string>.Failure)result;
        Assert.Equal("TOKEN_INVALID", failureResult.Code);
    }

    [Fact]
    public void GenerateTokenAsync_SecretKeyTooShort_ThrowsInvalidOperationException()
    {
        // Arrange
        const string shortKey = "tooshort";
        var jwtOptions = new JwtOptions
        {
            SecretKey = shortKey,
            Issuer = ValidIssuer,
            Audience = ValidAudience,
            Enabled = true
        };

        var options = new AuthOptions { Jwt = jwtOptions };
        var mockLogger = new Mock<ILogger<JwtAuthService>>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new JwtAuthService(Options.Create(options), mockLogger.Object));
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsAuthUserWithCorrectProperties()
    {
        // Arrange
        var service = CreateService();
        var userId = "user-456";
        var email = "john@example.com";
        var roles = new[] { "admin" };
        var generateResult = await service.GenerateTokenAsync(userId, email, roles);
        var tokenResult = (Result<string>.Success)generateResult;

        // Act
        var validationResult = await service.ValidateTokenAsync(tokenResult.Data);

        // Assert
        Assert.IsType<Result<IAuthUser>.Success>(validationResult);
        var authUserResult = (Result<IAuthUser>.Success)validationResult;
        var authUser = authUserResult.Data;

        Assert.Equal(userId, authUser.Id);
        Assert.Equal(email, authUser.Email);
        Assert.Single(authUser.Roles);
        Assert.Contains("admin", authUser.Roles);
        Assert.True(authUser.IsAuthenticated);
    }

    [Fact]
    public async Task GenerateTokenAsync_WithEmptyRoles_TokenGeneratesSuccessfully()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GenerateTokenAsync("user-123", "user@example.com", []);

        // Assert
        Assert.IsType<Result<string>.Success>(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithClockSkew_ValidatesTokensWithinSkewWindow()
    {
        // Arrange
        var service = CreateService(validationCustomizer: opts => opts.ClockSkewSeconds = 60);

        // Generate token that will expire
        var generateResult = await service.GenerateTokenAsync(
            "user-123",
            "user@example.com",
            [],
            TimeSpan.FromSeconds(1)); // Expire in 1 second
        var tokenResult = (Result<string>.Success)generateResult;

        // Wait for token to technically expire but within skew window
        await Task.Delay(1100);

        // Act
        var validationResult = await service.ValidateTokenAsync(tokenResult.Data);

        // Assert
        // With clock skew of 60 seconds, the token should still be valid
        Assert.IsType<Result<IAuthUser>.Success>(validationResult);
    }

    [Fact]
    public async Task MultipleTokens_CanBeGeneratedAndTracked()
    {
        // Arrange
        var service = CreateService();
        var tokens = new List<string>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            var result = await service.GenerateTokenAsync($"user-{i}", $"user{i}@example.com", []);
            var tokenResult = (Result<string>.Success)result;
            tokens.Add(tokenResult.Data);
        }

        // Assert
        Assert.Equal(5, tokens.Count);
        var handler = new JwtSecurityTokenHandler();

        // Verify each token is unique (different jti)
        var jtis = tokens.Select(t => handler.ReadJwtToken(t).Id).Distinct().ToList();
        Assert.Equal(5, jtis.Count);
    }

    [Fact]
    public async Task RevokeToken_Idempotent_CanRevokeMultipleTimes()
    {
        // Arrange
        var service = CreateService();
        var generateResult = await service.GenerateTokenAsync("user-123", "user@example.com", []);
        var tokenResult = (Result<string>.Success)generateResult;

        // Act
        var firstRevoke = await service.RevokeTokenAsync(tokenResult.Data);
        var secondRevoke = await service.RevokeTokenAsync(tokenResult.Data);

        // Assert
        Assert.IsType<Result.Success>(firstRevoke);
        Assert.IsType<Result.Success>(secondRevoke);
    }
}
