using SharedCommon.Auth;

namespace SharedCommon.Testing;

/// <summary>
/// Authenticated <see cref="ICurrentUser"/> for unit tests.
/// Construct with the user ID, email, and optional roles needed for the test scenario.
/// </summary>
public sealed class TestCurrentUser : ICurrentUser
{
    /// <summary>Creates an authenticated test user.</summary>
    /// <param name="userId">The user's unique identifier (maps to the JWT <c>sub</c> claim).</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="roles">Optional roles assigned to this user.</param>
    public TestCurrentUser(string userId, string email = "test@example.com", params string[] roles)
    {
        UserId = userId;
        Email = email;
        Roles = roles;
        User = new AuthUser
        {
            Id = userId,
            Email = email,
            Roles = roles,
            Permissions = Permissions
        };
    }

    /// <inheritdoc />
    public IAuthUser User { get; }

    /// <inheritdoc />
    public bool IsAuthenticated => true;

    /// <inheritdoc />
    public string? UserId { get; }

    /// <inheritdoc />
    public string? Email { get; }

    /// <inheritdoc />
    public IEnumerable<string> Roles { get; }

    /// <inheritdoc />
    public IEnumerable<string> Permissions { get; } = [];

    /// <summary>Creates an admin user for tests that require elevated permissions.</summary>
    public static TestCurrentUser Admin(string userId = "admin-user-id") =>
        new(userId, "admin@example.com", "Admin");
}
