using System;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Permissions.Admin;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Staff/role operations. These are the writes that can grant capabilities — including the one that
/// grants capabilities — so every one is audited with the operator's reason like the rest, and the
/// endpoint behind them carries its own <c>OpsStaffManage</c> capability rather than sharing an
/// ops capability with content editing.
/// </summary>
internal sealed partial class DashboardOperationsService
{
    public Task<OperationResult> CreateRoleAsync(
        CreateRoleRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.staff.role.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.Key, request.Name },
            work: async c =>
                Throw(
                    await _staffAdmin
                        .CreateRoleAsync(new RoleSpec(request.Key, request.Name), c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> UpdateRoleAsync(
        UpdateRoleRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.staff.role.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.RoleId,
                request.Key,
                request.Name,
            },
            work: async c =>
                Throw(
                    await _staffAdmin
                        .UpdateRoleAsync(request.RoleId, new RoleSpec(request.Key, request.Name), c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> DeleteRoleAsync(
        DeleteRoleRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.staff.role.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.RoleId },
            work: async c =>
                Throw(await _staffAdmin.DeleteRoleAsync(request.RoleId, c).ConfigureAwait(false)),
            ct
        );

    public Task<OperationResult> SetRoleCapabilitiesAsync(
        SetRoleCapabilitiesRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.staff.role.capabilities",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            // The whole set is recorded, not the delta: the audit entry has to answer "what could
            // this role do after the change" on its own.
            detail: new { request.RoleId, request.Capabilities },
            work: async c =>
                Throw(
                    await _staffAdmin
                        .SetRoleCapabilitiesAsync(
                            request.RoleId,
                            new RoleCapabilitiesSpec(request.Capabilities),
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> AssignRoleAsync(
        AssignRoleRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.staff.role.assign",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.AccountId, request.RoleId },
            work: async c =>
                Throw(
                    await _staffAdmin
                        .AssignRoleAsync(request.AccountId, request.RoleId, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> UnassignRoleAsync(
        AssignRoleRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.staff.role.unassign",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.AccountId, request.RoleId },
            work: async c =>
                Throw(
                    await _staffAdmin
                        .UnassignRoleAsync(request.AccountId, request.RoleId, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> CreateSanctionPresetAsync(
        CreateSanctionPresetRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.staff.preset.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.Kind,
                request.PresetIndex,
                request.Name,
            },
            work: async c =>
                Throw(
                    await _staffAdmin
                        .CreateSanctionPresetAsync(
                            new SanctionPresetSpec(
                                ToPresetKind(request.Kind),
                                request.PresetIndex,
                                request.Name,
                                request.DurationSeconds,
                                request.Message
                            ),
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> UpdateSanctionPresetAsync(
        UpdateSanctionPresetRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.staff.preset.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.PresetId, request.Name },
            work: async c =>
                Throw(
                    await _staffAdmin
                        .UpdateSanctionPresetAsync(
                            request.PresetId,
                            new SanctionPresetSpec(
                                ToPresetKind(request.Kind),
                                request.PresetIndex,
                                request.Name,
                                request.DurationSeconds,
                                request.Message
                            ),
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> DeleteSanctionPresetAsync(
        DeleteSanctionPresetRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.staff.preset.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.PresetId },
            work: async c =>
                Throw(
                    await _staffAdmin
                        .DeleteSanctionPresetAsync(request.PresetId, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    private static SanctionPresetKind ToPresetKind(int value) =>
        Enum.IsDefined(typeof(SanctionPresetKind), value)
            ? (SanctionPresetKind)value
            : throw new InvalidOperationException("invalid_preset_kind");

    private static void Throw(StaffAdminResult result)
    {
        if (!result.Success)
        {
            throw new InvalidOperationException(result.ErrorCode);
        }
    }
}
