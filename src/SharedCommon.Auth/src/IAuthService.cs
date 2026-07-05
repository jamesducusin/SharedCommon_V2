using SharedCommon.Core;

namespace SharedCommon.Auth;

/// <summary>
/// Authentication service for token generation, validation, and revocation.
///
/// Example:
/// <code>
/// var result = await _authService.GenerateTokenAsync(
///     userId: user.Id,
///     email: user.Email,
///     roles: user.Roles,
///     ct: ct);
///
/// return result switch
/// {
///     Result&lt;string&gt;.Success s => Ok(new { Token = s.Data }),
///     Result&lt;string&gt;.Failure f => BadRequest(f.Message),
///     _ => StatusCode(500)
/// };
/// </code>
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Validates a JWT token and returns the decoded principal.
    /// Returns <see cref="Result{T}.Failure"/> when the token is invalid, expired, or blacklisted.
    /// </summary>
    /// <param name="token">The raw JWT string.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<IAuthUser>> ValidateTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Generates a signed JWT token for the specified user.
    /// Returns <see cref="Result{T}.Failure"/> when configuration is invalid.
    /// </summary>
    /// <param name="userId">Unique user identifier (written as the <c>sub</c> claim).</param>
    /// <param name="email">User email address.</param>
    /// <param name="roles">Roles to embed as claims.</param>
    /// <param name="expiration">Token lifetime. <c>null</c> uses <see cref="JwtOptions.ExpirationMinutes"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<string>> GenerateTokenAsync(
        string userId,
        string email,
        IEnumerable<string> roles,
        TimeSpan? expiration = null,
        CancellationToken ct = default);

    /// <summary>
    /// Exchanges a refresh token for a new access token.
    /// Returns <see cref="Result{T}.Failure"/> when the refresh token is invalid or expired.
    /// </summary>
    /// <param name="refreshToken">The refresh token previously issued by <see cref="GenerateTokenAsync"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result<string>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Adds the token to the blacklist so it can no longer be used.
    /// No-ops if the token is already expired or blacklisted.
    /// </summary>
    /// <param name="token">The raw JWT string to revoke.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result> RevokeTokenAsync(string token, CancellationToken ct = default);
}
