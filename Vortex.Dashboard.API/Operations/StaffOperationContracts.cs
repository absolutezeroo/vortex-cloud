using System.Collections.Generic;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Request bodies for the staff/role operations, each carrying a mandatory audited <c>Reason</c>.
/// <c>Capabilities</c> on the role update is the complete set — anything absent is revoked.
/// </summary>
public sealed record CreateRoleRequest(string Key, string Name, string Reason);

public sealed record UpdateRoleRequest(int RoleId, string Key, string Name, string Reason);

public sealed record DeleteRoleRequest(int RoleId, string Reason);

public sealed record SetRoleCapabilitiesRequest(
    int RoleId,
    IReadOnlyCollection<string> Capabilities,
    string Reason
);

public sealed record AssignRoleRequest(int AccountId, int RoleId, string Reason);

public sealed record CreateSanctionPresetRequest(
    int Kind,
    int PresetIndex,
    string Name,
    int? DurationSeconds,
    string? Message,
    string Reason
);

public sealed record UpdateSanctionPresetRequest(
    int PresetId,
    int Kind,
    int PresetIndex,
    string Name,
    int? DurationSeconds,
    string? Message,
    string Reason
);

public sealed record DeleteSanctionPresetRequest(int PresetId, string Reason);
