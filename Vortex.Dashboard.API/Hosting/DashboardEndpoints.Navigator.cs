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
            async (
                HttpContext ctx,
                CreateNavigatorContextRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.SearchCode)
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
            async (
                HttpContext ctx,
                UpdateNavigatorContextRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.ContextId <= 0 || string.IsNullOrWhiteSpace(body.SearchCode)
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
            async (
                HttpContext ctx,
                DeleteNavigatorContextRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.ContextId <= 0
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
            async (
                HttpContext ctx,
                CreateNavigatorQuickLinkRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.ContextId <= 0 || string.IsNullOrWhiteSpace(body.SearchCode)
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
            async (
                HttpContext ctx,
                UpdateNavigatorQuickLinkRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.QuickLinkId <= 0
                || body.ContextId <= 0
                || string.IsNullOrWhiteSpace(body.SearchCode)
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
            async (
                HttpContext ctx,
                DeleteNavigatorQuickLinkRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.QuickLinkId <= 0
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
            async (
                HttpContext ctx,
                CreateNavigatorFlatCategoryRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.Name)
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
            async (
                HttpContext ctx,
                UpdateNavigatorFlatCategoryRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.CategoryId <= 0 || string.IsNullOrWhiteSpace(body.Name)
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
            async (
                HttpContext ctx,
                DeleteNavigatorFlatCategoryRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.CategoryId <= 0
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
            async (
                HttpContext ctx,
                CreateNavigatorEventCategoryRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.Name)
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
            async (
                HttpContext ctx,
                UpdateNavigatorEventCategoryRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.CategoryId <= 0 || string.IsNullOrWhiteSpace(body.Name)
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
            async (
                HttpContext ctx,
                DeleteNavigatorEventCategoryRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                body.CategoryId <= 0
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
            async (
                HttpContext ctx,
                SeedNavigatorDefaultsRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                Results.Ok(
                    await ops.SeedNavigatorDefaultsAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                ),
            Capabilities.Dashboard.OpsNavigatorManage,
            TagNavigator
        );
    }
}
