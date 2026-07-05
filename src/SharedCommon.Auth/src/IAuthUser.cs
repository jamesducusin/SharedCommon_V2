namespace SharedCommon.Auth;

/// <summary>
/// Represents an authenticated user as decoded from a JWT token.
/// Read-only view of claims; use <see cref="ICurrentUser"/> for ambient access inside request handlers.
/// </summary>
public interface IAuthUser
{
    /// <summary>Unique user identifier (the JWT <c>sub</c> claim).</summary>
    string Id { get; }

    /// <summary>User email address.</summary>
    string Email { get; }

    /// <summary>Roles assigned to the user.</summary>
    IEnumerable<string> Roles { get; }

    /// <summary>Fine-grained permissions granted to the user.</summary>
    IEnumerable<string> Permissions { get; }

    /// <summary>All JWT claims as a key-value dictionary.</summary>
    IDictionary<string, object> Claims { get; }

    /// <summary><c>true</c> when the user's identity has been verified.</summary>
    bool IsAuthenticated { get; }
}

/// <summary>
/// Default <see cref="IAuthUser"/> implementation populated from JWT claims.
/// </summary>
public sealed class AuthUser : IAuthUser
{
    /// <inheritdoc />
    public string Id { get; init; } = string.Empty;

    /// <inheritdoc />
    public string Email { get; init; } = string.Empty;

    /// <inheritdoc />
    public IEnumerable<string> Roles { get; init; } = [];

    /// <inheritdoc />
    public IEnumerable<string> Permissions { get; init; } = [];

    /// <inheritdoc />
    public IDictionary<string, object> Claims { get; init; } = new Dictionary<string, object>();

    /// <inheritdoc />
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Id);
}
