using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedCommon.Core;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SharedCommon.Auth;

/// <summary>
/// HS256 JWT implementation of <see cref="IAuthService"/>.
/// Includes in-memory token blacklisting for revocation.
/// </summary>
public sealed class JwtAuthService : IAuthService
{
    private readonly AuthOptions _options;
    private readonly ILogger<JwtAuthService> _logger;
    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly TokenValidationParameters _validationParams;

    // In-memory blacklist: token jti → expiry. Entries are lazily purged on validation.
    private readonly ConcurrentDictionary<string, DateTimeOffset> _blacklist = new();

    /// <summary>Initializes the JWT auth service.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the secret key is too short.</exception>
    public JwtAuthService(IOptions<AuthOptions> options, ILogger<JwtAuthService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;

        if (_options.Jwt.Enabled && _options.Jwt.SecretKey.Length < 32)
            throw new InvalidOperationException(
                "JWT SecretKey must be at least 32 characters. Store it in user secrets or a vault.");

        _validationParams = BuildValidationParameters();
    }

    /// <inheritdoc />
    public Task<Result<IAuthUser>> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Task.FromResult(Result<IAuthUser>.Fail("TOKEN_INVALID", "Token is required."));

        try
        {
            var principal = _handler.ValidateToken(token, _validationParams, out var validatedToken);

            var jti = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);

            // Purge expired blacklist entries and check if this token is revoked.
            PurgeExpiredBlacklistEntries();
            if (jti is not null && _blacklist.ContainsKey(jti))
                return Task.FromResult(Result<IAuthUser>.Fail("TOKEN_REVOKED", "Token has been revoked."));

            var user = MapToAuthUser(principal);
            return Task.FromResult(Result<IAuthUser>.Ok(user));
        }
        catch (SecurityTokenExpiredException)
        {
            return Task.FromResult(Result<IAuthUser>.Fail("TOKEN_EXPIRED", "Token has expired."));
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return Task.FromResult(Result<IAuthUser>.Fail("TOKEN_INVALID", "Token signature is invalid."));
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return Task.FromResult(Result<IAuthUser>.Fail("TOKEN_INVALID", "Token is invalid."));
        }
    }

    /// <inheritdoc />
    public Task<Result<string>> GenerateTokenAsync(
        string userId,
        string email,
        IEnumerable<string> roles,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(Result<string>.Fail("INVALID_USER_ID", "UserId is required."));

        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Jwt.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.Add(
                expiration ?? TimeSpan.FromMinutes(_options.Jwt.ExpirationMinutes));

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            if (_options.Jwt.Claims.IncludeEmail && !string.IsNullOrWhiteSpace(email))
                claims.Add(new Claim(ClaimTypes.Email, email));

            if (_options.Jwt.Claims.IncludeRoles)
                claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var tokenDescriptor = new JwtSecurityToken(
                issuer: _options.Jwt.Issuer,
                audience: _options.Jwt.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: credentials);

            var tokenString = _handler.WriteToken(tokenDescriptor);
            return Task.FromResult(Result<string>.Ok(tokenString));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate JWT token for user {UserId}", userId);
            return Task.FromResult(Result<string>.Fail("TOKEN_GENERATION_FAILED", "Failed to generate token.", ex));
        }
    }

    /// <inheritdoc />
    public Task<Result<string>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        // Full refresh token flow requires a persistent store. This implementation
        // validates the existing token leniently (ignoring lifetime) and reissues it.
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Task.FromResult(Result<string>.Fail("TOKEN_INVALID", "Refresh token is required."));

        var relaxedParams = _validationParams.Clone();
        relaxedParams.ValidateLifetime = false;

        try
        {
            var principal = _handler.ValidateToken(refreshToken, relaxedParams, out _);
            var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                         ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value);

            if (string.IsNullOrWhiteSpace(userId))
                return Task.FromResult(Result<string>.Fail("TOKEN_INVALID", "Token is missing sub claim."));

            return GenerateTokenAsync(userId, email, roles, ct: ct);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Refresh token validation failed");
            return Task.FromResult(Result<string>.Fail("TOKEN_INVALID", "Refresh token is invalid."));
        }
    }

    /// <inheritdoc />
    public Task<Result> RevokeTokenAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Task.FromResult<Result>(new Result.Failure("TOKEN_INVALID", "Token is required."));

        try
        {
            var jwtToken = _handler.ReadJwtToken(token);
            var jti = jwtToken.Id;

            if (!string.IsNullOrWhiteSpace(jti))
                _blacklist[jti] = jwtToken.ValidTo == DateTime.MinValue
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : new DateTimeOffset(jwtToken.ValidTo, TimeSpan.Zero);

            return Task.FromResult<Result>(new Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to revoke token");
            return Task.FromResult<Result>(new Result.Failure("REVOKE_FAILED", "Failed to revoke token.", ex));
        }
    }

    private TokenValidationParameters BuildValidationParameters()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Jwt.SecretKey));

        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = _options.Jwt.Validation.ValidateIssuer,
            ValidIssuer = _options.Jwt.Issuer,
            ValidateAudience = _options.Jwt.Validation.ValidateAudience,
            ValidAudience = _options.Jwt.Audience,
            ValidateLifetime = _options.Jwt.Validation.ValidateLifetime,
            ClockSkew = TimeSpan.FromSeconds(_options.Jwt.Validation.ClockSkewSeconds)
        };
    }

    private static IAuthUser MapToAuthUser(ClaimsPrincipal principal) => new AuthUser
    {
        Id = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
             ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
             ?? string.Empty,
        Email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
        Roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
        Permissions = principal.FindAll("permission").Select(c => c.Value).ToList(),
        Claims = principal.Claims.ToDictionary(c => c.Type, c => (object)c.Value)
    };

    private void PurgeExpiredBlacklistEntries()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var key in _blacklist.Keys.ToList())
        {
            if (_blacklist.TryGetValue(key, out var expiry) && expiry < now)
                _blacklist.TryRemove(key, out _);
        }
    }
}
