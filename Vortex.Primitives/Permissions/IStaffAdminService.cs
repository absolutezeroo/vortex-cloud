using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Permissions.Admin;

namespace Vortex.Primitives.Permissions;

/// <summary>
/// Writes for the staff roster: roles, the capabilities they grant, who holds them, and the sanction
/// preset ladder.
/// <para>
/// Every write that changes what an account may do invalidates that account in
/// <see cref="IPermissionService"/>. Its cache has a 60-second TTL, so skipping the invalidation
/// does not lose the change — it silently delays it by up to a minute, which reads as "the dashboard
/// did nothing" to whoever just granted themselves a capability.
/// </para>
/// </summary>
public interface IStaffAdminService
{
    Task<StaffAdminResult> CreateRoleAsync(RoleSpec spec, CancellationToken ct);

    Task<StaffAdminResult> UpdateRoleAsync(int roleId, RoleSpec spec, CancellationToken ct);

    Task<StaffAdminResult> DeleteRoleAsync(int roleId, CancellationToken ct);

    /// <summary>Replaces a role's capability set outright — anything absent is revoked.</summary>
    Task<StaffAdminResult> SetRoleCapabilitiesAsync(
        int roleId,
        RoleCapabilitiesSpec spec,
        CancellationToken ct
    );

    Task<StaffAdminResult> AssignRoleAsync(int accountId, int roleId, CancellationToken ct);

    Task<StaffAdminResult> UnassignRoleAsync(int accountId, int roleId, CancellationToken ct);

    Task<StaffAdminResult> CreateSanctionPresetAsync(SanctionPresetSpec spec, CancellationToken ct);

    Task<StaffAdminResult> UpdateSanctionPresetAsync(
        int presetId,
        SanctionPresetSpec spec,
        CancellationToken ct
    );

    Task<StaffAdminResult> DeleteSanctionPresetAsync(int presetId, CancellationToken ct);
}
