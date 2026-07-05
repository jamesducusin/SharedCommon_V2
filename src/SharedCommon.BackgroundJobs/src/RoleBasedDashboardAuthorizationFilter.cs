using Hangfire.Dashboard;

namespace SharedCommon.BackgroundJobs;

/// <summary>
/// Restricts the Hangfire dashboard to authenticated users with a specific role.
/// Configured via <see cref="BackgroundJobOptions.DashboardRequiredRole"/>.
/// </summary>
internal sealed class RoleBasedDashboardAuthorizationFilter(string requiredRole) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            return false;

        return string.IsNullOrEmpty(requiredRole)
               || httpContext.User.IsInRole(requiredRole);
    }
}
