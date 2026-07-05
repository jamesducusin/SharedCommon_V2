using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace SharedCommon.MultiTenancy;

/// <summary>
/// Default <see cref="ITenantResolver"/> implementation. Resolves tenant ID from the
/// source configured in <see cref="MultiTenancyOptions.Strategy"/>.
/// </summary>
internal sealed class DefaultTenantResolver(IOptions<MultiTenancyOptions> options) : ITenantResolver
{
    private readonly MultiTenancyOptions _options = options.Value;

    public Task<TenantInfo?> ResolveAsync(HttpContext context, CancellationToken ct = default)
    {
        var tenantId = _options.Strategy switch
        {
            TenantResolutionStrategy.Header => ResolveFromHeader(context),
            TenantResolutionStrategy.Claim => ResolveFromClaim(context),
            TenantResolutionStrategy.Subdomain => ResolveFromSubdomain(context),
            TenantResolutionStrategy.QueryString => ResolveFromQueryString(context),
            _ => null
        };

        var result = tenantId is { Length: > 0 }
            ? new TenantInfo(tenantId)
            : null;

        return Task.FromResult(result);
    }

    private string? ResolveFromHeader(HttpContext context) =>
        context.Request.Headers.TryGetValue(_options.HeaderName, out var value)
            ? value.ToString()
            : null;

    private string? ResolveFromClaim(HttpContext context) =>
        context.User.FindFirst(_options.ClaimName)?.Value;

    private static string? ResolveFromSubdomain(HttpContext context)
    {
        var host = context.Request.Host.Host;
        var parts = host.Split('.');
        return parts.Length >= 3 ? parts[0] : null;
    }

    private string? ResolveFromQueryString(HttpContext context) =>
        context.Request.Query.TryGetValue(_options.QueryStringKey, out var value)
            ? value.ToString()
            : null;
}
