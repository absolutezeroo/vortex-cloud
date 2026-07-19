using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Security;

/// <summary>
/// The authenticated dashboard caller: the backing account and its resolved effective capabilities.
/// Authorization is capability-based (see <see cref="Vortex.Primitives.Permissions.Capabilities"/>);
/// there are no dashboard-specific roles.
/// </summary>
internal sealed record DashboardPrincipal(int AccountId, string Email, PermissionSet Permissions)
{
    public bool Has(string capability) => Permissions.Has(capability);
}
