using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Operations;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// Staff/role write surface. Reads live in <c>DashboardEndpoints.Insights.cs</c>; the writes are
/// here behind their own <see cref="Capabilities.Dashboard.OpsStaffManage"/> because this is the
/// only group of endpoints that can hand out capabilities.
/// </summary>
internal static partial class DashboardEndpoints
{
    private const string OpsStaff = ApiOperations + "/staff";

    public static void MapStaffOperations(WebApplication app)
    {
        MapPost(
            app,
            OpsStaff + "/roles",
            "/api/operations/staff/roles",
            async (
                HttpContext ctx,
                CreateRoleRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.Key) || string.IsNullOrWhiteSpace(body.Name)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreateRoleAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsStaffManage,
            TagStaff
        );
        MapPost(
            app,
            OpsStaff + "/roles/update",
            "/api/operations/staff/roles/update",
            async (
                HttpContext ctx,
                UpdateRoleRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.RoleId <= 0
                || string.IsNullOrWhiteSpace(body.Key)
                || string.IsNullOrWhiteSpace(body.Name)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdateRoleAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsStaffManage,
            TagStaff
        );
        MapPost(
            app,
            OpsStaff + "/roles/delete",
            "/api/operations/staff/roles/delete",
            async (
                HttpContext ctx,
                DeleteRoleRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.RoleId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteRoleAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsStaffManage,
            TagStaff
        );
        MapPost(
            app,
            OpsStaff + "/roles/capabilities",
            "/api/operations/staff/roles/capabilities",
            async (
                HttpContext ctx,
                SetRoleCapabilitiesRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.RoleId <= 0 || body.Capabilities is null
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.SetRoleCapabilitiesAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsStaffManage,
            TagStaff
        );
        MapPost(
            app,
            OpsStaff + "/assignments",
            "/api/operations/staff/assignments",
            async (
                HttpContext ctx,
                AssignRoleRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.AccountId <= 0 || body.RoleId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.AssignRoleAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsStaffManage,
            TagStaff
        );
        MapPost(
            app,
            OpsStaff + "/assignments/delete",
            "/api/operations/staff/assignments/delete",
            async (
                HttpContext ctx,
                AssignRoleRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.AccountId <= 0 || body.RoleId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UnassignRoleAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsStaffManage,
            TagStaff
        );
        MapPost(
            app,
            OpsStaff + "/presets",
            "/api/operations/staff/presets",
            async (
                HttpContext ctx,
                CreateSanctionPresetRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.Name) || body.PresetIndex < 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreateSanctionPresetAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsStaffManage,
            TagStaff
        );
        MapPost(
            app,
            OpsStaff + "/presets/update",
            "/api/operations/staff/presets/update",
            async (
                HttpContext ctx,
                UpdateSanctionPresetRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.PresetId <= 0 || string.IsNullOrWhiteSpace(body.Name) || body.PresetIndex < 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdateSanctionPresetAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsStaffManage,
            TagStaff
        );
        MapPost(
            app,
            OpsStaff + "/presets/delete",
            "/api/operations/staff/presets/delete",
            async (
                HttpContext ctx,
                DeleteSanctionPresetRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.PresetId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteSanctionPresetAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsStaffManage,
            TagStaff
        );
    }
}
