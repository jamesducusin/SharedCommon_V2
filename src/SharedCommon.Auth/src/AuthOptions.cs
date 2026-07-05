using System.ComponentModel.DataAnnotations;

namespace SharedCommon.Auth;

/// <summary>
/// Top-level configuration for SharedCommon authentication.
///
/// Configure via appsettings.json (secrets in user-secrets or vault):
/// <code>
/// {
///   "SharedCommon": {
///     "Auth": {
///       "Jwt": {
///         "SecretKey": "your-secret-min-32-chars",
///         "Issuer": "https://auth.example.com",
///         "Audience": "https://api.example.com"
///       }
///     }
///   }
/// }
/// </code>
/// </summary>
public sealed class AuthOptions
{
    /// <summary>Configuration section key: <c>SharedCommon:Auth</c>.</summary>
    public const string SectionName = "SharedCommon:Auth";

    /// <summary>JWT settings.</summary>
    public JwtOptions Jwt { get; set; } = new();

    /// <summary>OAuth 2.0 / OpenID Connect settings.</summary>
    public OAuthOptions OAuth { get; set; } = new();

    /// <summary>Token blacklist / revocation settings.</summary>
    public TokenBlacklistOptions TokenBlacklist { get; set; } = new();

    /// <summary>Password complexity policy settings.</summary>
    public PasswordPolicyOptions PasswordPolicy { get; set; } = new();
}

/// <summary>JWT token generation and validation settings.</summary>
public sealed class JwtOptions
{
    /// <summary>Enable JWT authentication. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Symmetric signing key. Required when enabled.
    /// Must be at least 32 characters. Store in secrets, never in appsettings.json.
    /// </summary>
    [MinLength(32, ErrorMessage = "SecretKey must be at least 32 characters.")]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Token issuer. Required when enabled.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Issuer is required.")]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Token audience. Required when enabled.</summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Audience is required.")]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Access token lifetime in minutes. Default: 60.</summary>
    [Range(1, 10080)]
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>Refresh token lifetime in days. Default: 7.</summary>
    [Range(1, 365)]
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>Signing algorithm. <c>HS256</c> | <c>RS256</c>. Default: <c>HS256</c>.</summary>
    public string Algorithm { get; set; } = "HS256";

    /// <summary>Claims included in generated tokens.</summary>
    public JwtClaimsOptions Claims { get; set; } = new();

    /// <summary>Token validation parameters.</summary>
    public JwtValidationOptions Validation { get; set; } = new();
}

/// <summary>Controls which standard claims are embedded in tokens.</summary>
public sealed class JwtClaimsOptions
{
    /// <summary>Include user email as a claim. Default: <c>true</c>.</summary>
    public bool IncludeEmail { get; set; } = true;

    /// <summary>Include roles as claims. Default: <c>true</c>.</summary>
    public bool IncludeRoles { get; set; } = true;

    /// <summary>Include permissions as claims. Default: <c>true</c>.</summary>
    public bool IncludePermissions { get; set; } = true;
}

/// <summary>JWT token validation parameters.</summary>
public sealed class JwtValidationOptions
{
    /// <summary>Validate the audience claim. Default: <c>true</c>.</summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>Validate the issuer claim. Default: <c>true</c>.</summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>Validate token expiry. Default: <c>true</c>.</summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>Clock skew tolerance in seconds. Default: 0 (strict).</summary>
    [Range(0, 300)]
    public int ClockSkewSeconds { get; set; } = 0;
}

/// <summary>OAuth 2.0 / OpenID Connect provider configuration.</summary>
public sealed class OAuthOptions
{
    /// <summary>Enable OAuth providers. Default: <c>false</c>.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Azure AD provider settings.</summary>
    public AzureAdOptions AzureAd { get; set; } = new();

    /// <summary>Google OAuth2 provider settings.</summary>
    public GoogleOAuthOptions Google { get; set; } = new();

    /// <summary>GitHub OAuth2 provider settings.</summary>
    public GitHubOAuthOptions Github { get; set; } = new();
}

/// <summary>Azure Active Directory settings.</summary>
public sealed class AzureAdOptions
{
    /// <summary>Enable Azure AD authentication. Default: <c>false</c>.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Azure AD tenant ID. Required when enabled.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Application (client) ID. Required when enabled.</summary>
    public string ClientId { get; set; } = string.Empty;
}

/// <summary>Google OAuth2 settings.</summary>
public sealed class GoogleOAuthOptions
{
    /// <summary>Enable Google OAuth. Default: <c>false</c>.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Client ID. Required when enabled.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Client secret. Required when enabled. Store in secrets.</summary>
    public string ClientSecret { get; set; } = string.Empty;
}

/// <summary>GitHub OAuth2 settings.</summary>
public sealed class GitHubOAuthOptions
{
    /// <summary>Enable GitHub OAuth. Default: <c>false</c>.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Client ID. Required when enabled.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Client secret. Required when enabled. Store in secrets.</summary>
    public string ClientSecret { get; set; } = string.Empty;
}

/// <summary>Token revocation / blacklist settings.</summary>
public sealed class TokenBlacklistOptions
{
    /// <summary>Enable token blacklisting. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Storage backend for revoked tokens. <c>Memory</c> | <c>Redis</c>. Default: <c>Redis</c>.</summary>
    public string Backend { get; set; } = "Redis";

    /// <summary>Skip validation of already-expired tokens (they'll be rejected by lifetime check anyway). Default: <c>true</c>.</summary>
    public bool CheckExpired { get; set; } = true;
}

/// <summary>Password complexity policy settings.</summary>
public sealed class PasswordPolicyOptions
{
    /// <summary>Enable password policy enforcement. Default: <c>true</c>.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Minimum password length. Default: 12.</summary>
    [Range(8, 128)]
    public int MinLength { get; set; } = 12;

    /// <summary>Require at least one uppercase letter. Default: <c>true</c>.</summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>Require at least one lowercase letter. Default: <c>true</c>.</summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary>Require at least one digit. Default: <c>true</c>.</summary>
    public bool RequireDigits { get; set; } = true;

    /// <summary>Require at least one special character. Default: <c>true</c>.</summary>
    public bool RequireSpecialChars { get; set; } = true;

    /// <summary>Password expiration in days. <c>null</c> disables expiration. Optional.</summary>
    public int? ExpirationDays { get; set; }
}
