using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace SharedCommon.Auth;

/// <summary>
/// Ambient service providing the currently authenticated user inside a request scope.
/// Inject into controllers or domain services to avoid passing ClaimsPrincipal manually.
///
/// Example:
/// <code>
/// public class OrderService
/// {
///     private readonly ICurrentUser _currentUser;
///
///     public async Task CreateOrderAsync(CreateOrderCommand cmd, CancellationToken ct)
///     {
///         if (!_currentUser.IsAuthenticated)
///             throw new UnauthorizedException();
///
///         cmd.UserId = _currentUser.UserId!;
///     }
/// }
/// </code>
/// </summary>
public interface ICurrentUser
{
    /// <summary>The authenticated user. May represent an anonymous user when not authenticated.</summary>
    IAuthUser User { get; }

    /// <summary><c>true</c> when the request carries valid authentication.</summary>
    bool IsAuthenticated { get; }

    /// <summary>User ID from the JWT <c>sub</c> claim. <c>null</c> when anonymous.</summary>
    string? UserId { get; }

    /// <summary>User email from the JWT <c>email</c> claim. <c>null</c> when anonymous.</summary>
    string? Email { get; }

    /// <summary>Roles assigned to the user. Empty when anonymous.</summary>
    IEnumerable<string> Roles { get; }

    /// <summary>Permissions granted to the user. Empty when anonymous.</summary>
    IEnumerable<string> Permissions { get; }
}

/// <summary>
/// Default <see cref="ICurrentUser"/> implementation backed by <see cref="IHttpContextAccessor"/>.
/// Registered as <c>Scoped</c> so it is re-resolved on each request.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Initializes CurrentUser with the HTTP context accessor.</summary>
    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public IAuthUser User => new AuthUser
    {
        Id = UserId ?? string.Empty,
        Email = Email ?? string.Empty,
        Roles = Roles,
        Permissions = Permissions,
        Claims = Principal?.Claims
            .ToDictionary(c => c.Type, c => (object)c.Value)
            ?? new Dictionary<string, object>()
    };

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    /// <inheritdoc />
    public string? UserId =>
        Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Principal?.FindFirstValue("sub");

    /// <inheritdoc />
    public string? Email =>
        Principal?.FindFirstValue(ClaimTypes.Email)
        ?? Principal?.FindFirstValue("email");

    /// <inheritdoc />
    public IEnumerable<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value)
        ?? Enumerable.Empty<string>();

    /// <inheritdoc />
    public IEnumerable<string> Permissions =>
        Principal?.FindAll("permission").Select(c => c.Value)
        ?? Enumerable.Empty<string>();
}
