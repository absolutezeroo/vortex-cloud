using System.Collections.Generic;
using Vortex.Dashboard.API.Hosting;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Request bodies for the staff/role operations, each carrying a mandatory audited <c>Reason</c>.
/// <c>Capabilities</c> on the role update is the complete set — anything absent is revoked.
/// </summary>
public sealed record CreateRoleRequest(string Key, string Name, string Reason) : IReasonedRequest;

public sealed record UpdateRoleRequest(int RoleId, string Key, string Name, string Reason)
    : IReasonedRequest;

public sealed record DeleteRoleRequest(int RoleId, string Reason) : IReasonedRequest;

public sealed record SetRoleCapabilitiesRequest(
    int RoleId,
    IReadOnlyCollection<string> Capabilities,
    string Reason
) : IReasonedRequest;

public sealed record AssignRoleRequest(int AccountId, int RoleId, string Reason) : IReasonedRequest;

/// <summary>
/// Clears another operator's second factor. This is the recovery path for a lost authenticator --
/// there are no one-time codes to keep in a drawer -- which makes it a way to strip a factor off an
/// account, so it lives behind OpsStaffManage and carries a reason like every other staff write.
/// </summary>
public sealed record ResetAccountMfaRequest(int AccountId, string Reason) : IReasonedRequest;

public sealed record CreateSanctionPresetRequest(
    int Kind,
    int PresetIndex,
    string Name,
    int? DurationSeconds,
    string? Message,
    string Reason
) : IReasonedRequest;

public sealed record UpdateSanctionPresetRequest(
    int PresetId,
    int Kind,
    int PresetIndex,
    string Name,
    int? DurationSeconds,
    string? Message,
    string Reason
) : IReasonedRequest;

public sealed record DeleteSanctionPresetRequest(int PresetId, string Reason) : IReasonedRequest;
