using System.Collections.Generic;

namespace Vortex.Primitives.Permissions.Admin;

/// <summary>Outcome of a staff/role write, same success/error-code shape as the other admin services.</summary>
public sealed record StaffAdminResult(bool Success, int? Id, string? ErrorCode)
{
    public static StaffAdminResult Ok(int id) => new(true, id, null);

    public static StaffAdminResult Fail(string errorCode) => new(false, null, errorCode);
}

/// <summary>
/// A role. <paramref name="Key"/> is the stable identifier code compares against (and what a
/// <c>PermissionSet</c> carries); <paramref name="Name"/> is only ever displayed.
/// </summary>
public sealed record RoleSpec(string Key, string Name);

/// <summary>
/// The complete capability set of a role — a replace, not a merge, so an unticked box actually
/// revokes. Keys are validated against <see cref="Capabilities.All"/>: a capability the code does
/// not declare would sit in the table granting nothing, which is the silent failure the staff page
/// exists to surface.
/// </summary>
public sealed record RoleCapabilitiesSpec(IReadOnlyCollection<string> Capabilities);

/// <summary>A sanction preset: one rung of the ladder the in-client mod tool offers.</summary>
public sealed record SanctionPresetSpec(
    SanctionPresetKind Kind,
    int PresetIndex,
    string Name,
    int? DurationSeconds,
    string? Message
);
