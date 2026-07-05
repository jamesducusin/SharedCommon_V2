using SharedCommon.Auth;

namespace SharedCommon.Testing;

/// <summary>
/// Represents an unauthenticated (anonymous) user. Use when the system-under-test
/// should behave as if no user is logged in.
/// </summary>
public sealed class NullCurrentUser : ICurrentUser
{
    /// <summary>Shared singleton instance.</summary>
    public static readonly NullCurrentUser Instance = new();

    private static readonly IAuthUser AnonymousUser = new AuthUser();

    /// <inheritdoc />
    public IAuthUser User => AnonymousUser;

    /// <inheritdoc />
    public bool IsAuthenticated => false;

    /// <inheritdoc />
    public string? UserId => null;

    /// <inheritdoc />
    public string? Email => null;

    /// <inheritdoc />
    public IEnumerable<string> Roles => [];

    /// <inheritdoc />
    public IEnumerable<string> Permissions => [];
}
