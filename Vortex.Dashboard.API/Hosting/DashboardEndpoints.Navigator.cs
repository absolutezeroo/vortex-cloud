using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Operations;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// Navigator configuration: one read returning the whole configuration (tabs, their blocks, both
/// category tables and the client's known search codes), and the CRUD that edits it. Every write
/// reloads the live navigator snapshot — see <c>INavigatorAdminService</c>.
/// </summary>
internal static partial class DashboardEndpoints
{
    private const string TagNavigator = "Navigator";
    private const string ApiNavigator = ApiV1 + "/navigator";
    private const string OpsNavigator = ApiOperations + "/navigator";

    public static void MapNavigatorReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiNavigator + "/config",
            "/api/navigator/config",
            (DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.NavigatorConfigAsync(ct)),
            Capabilities.Dashboard.NavigatorRead,
            TagNavigator
        );
    }

    public static void MapNavigatorOperations(WebApplication app)
    {
        MapPost(
            app,
            OpsNavigator + "/contexts",
            "/api/operations/navigator/contexts",
            async (
                HttpContext ctx,
                CreateNavigatorContextRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body is null
                || string.IsNullOrWhiteSpace(body.SearchCode)
                || !HasReason(body.Reason)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreateNavigatorContextAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsNavigatorManage,
            TagNavigator
        );
        MapPost(
            app,
            OpsNavigator + "/contexts/update",
            "/api/operations/navigator/contexts/update",
            async (
                HttpContext ctx,
                UpdateNavigatorContextRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body is null
                || body.ContextId <= 0
                || string.IsNullOrWhiteSpace(body.SearchCode)
                || !HasReason(body.Reason)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdateNavigatorContextAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsNavigatorManage,
            TagNavigator
        );
        MapPost(
            app,
            OpsNavigator + "/contexts/delete",
            "/api/operations/navigator/contexts/delete",
            async (
                HttpContext ctx,
                DeleteNavigatorContextRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body is null || body.ContextId <= 0 || !HasReason(body.Reason)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteNavigatorContextAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsNavigatorManage,
            TagNavigator
        );
        MapPost(
            app,
            OpsNavigator + "/quick-links",
            "/api/operations/navigator/quick-links",
            async (
                HttpContext ctx,
                CreateNavigatorQuickLinkRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body is null
                || body.ContextId <= 0
                || string.IsNullOrWhiteSpace(body.SearchCode)
                || !HasReason(body.Reason)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreateNavigatorQuickLinkAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsNavigatorManage,
            TagNavigator
        );
        MapPost(
            app,
            OpsNavigator + "/quick-links/update",
            "/api/operations/navigator/quick-links/update",
            async (
                HttpContext ctx,
                UpdateNavigatorQuickLinkRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body is null
                || body.QuickLinkId <= 0
                || body.ContextId <= 0
                || string.IsNullOrWhiteSpace(body.SearchCode)
                || !HasReason(body.Reason)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdateNavigatorQuickLinkAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsNavigatorManage,
            TagNavigator
        );
        MapPost(
            app,
            OpsNavigator + "/quick-links/delete",
            "/api/operations/navigator/quick-links/delete",
            async (
                HttpContext ctx,
                DeleteNavigatorQuickLinkRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body is null || body.QuickLinkId <= 0 || !HasReason(body.Reason)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteNavigatorQuickLinkAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsNavigatorManage,
            TagNavigator
        );
        MapPost(
            app,
            OpsNavigator + "/categories",
            "/api/operations/navigator/categories",
            async (
                HttpContext ctx,
                CreateNavigatorFlatCategoryRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body is null || string.IsNullOrWhiteSpace(body.Name) || !HasReason(body.Reason)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreateNavigatorFlatCategoryAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsNavigatorManage,
            TagNavigator
        );
        MapPost(
            app,
            OpsNavigator + "/categories/update",
            "/api/operations/navigator/categories/update",
            async (
                HttpContext ctx,
                UpdateNavigatorFlatCategoryRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body is null
                || body.CategoryId <= 0
                || string.IsNullOrWhiteSpace(body.Name)
                || !HasReason(body.Reason)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdateNavigatorFlatCategoryAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsNavigatorManage,
            TagNavigator
        );
        MapPost(
            app,
            OpsNavigator + "/categories/delete",
            "/api/operations/navigator/categories/delete",
            async (
                HttpContext ctx,
                DeleteNavigatorFlatCategoryRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body is null || body.CategoryId <= 0 || !HasReason(body.Reason)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteNavigatorFlatCategoryAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsNavigatorManage,
            TagNavigator
        );
        MapPost(
            app,
            OpsNavigator + "/event-categories",
            "/api/operations/navigator/event-categories",
            async (
                HttpContext ctx,
                CreateNavigatorEventCategoryRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body is null || string.IsNullOrWhiteSpace(body.Name) || !HasReason(body.Reason)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreateNavigatorEventCategoryAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsNavigatorManage,
            TagNavigator
        );
        MapPost(
            app,
            OpsNavigator + "/event-categories/update",
            "/api/operations/navigator/event-categories/update",
            async (
                HttpContext ctx,
                UpdateNavigatorEventCategoryRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body is null
                || body.CategoryId <= 0
                || string.IsNullOrWhiteSpace(body.Name)
                || !HasReason(body.Reason)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdateNavigatorEventCategoryAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsNavigatorManage,
            TagNavigator
        );
        MapPost(
            app,
            OpsNavigator + "/event-categories/delete",
            "/api/operations/navigator/event-categories/delete",
            async (
                HttpContext ctx,
                DeleteNavigatorEventCategoryRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body is null || body.CategoryId <= 0 || !HasReason(body.Reason)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteNavigatorEventCategoryAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsNavigatorManage,
            TagNavigator
        );
        MapPost(
            app,
            OpsNavigator + "/seed-defaults",
            "/api/operations/navigator/seed-defaults",
            async (
                HttpContext ctx,
                SeedNavigatorDefaultsRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body is null || !HasReason(body.Reason)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.SeedNavigatorDefaultsAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsNavigatorManage,
            TagNavigator
        );
    }
}
