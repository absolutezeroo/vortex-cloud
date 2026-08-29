using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Operations;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// The four gamedata files the game client downloads, read and written from the dashboard.
/// </summary>
/// <remarks>
/// Read and write are one capability here rather than the usual pair. There is nothing to look at on
/// these pages except the values themselves, and an operator who can be trusted to read 12 529 raw
/// client strings is the operator who edits them.
/// </remarks>
internal static partial class DashboardEndpoints
{
    private const string TagGamedata = "Gamedata";
    private const string ApiGamedata = ApiV1 + "/gamedata";

    public static void MapGamedataReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiGamedata,
            (DashboardApiService api) => Results.Ok(api.GamedataFiles()),
            Capabilities.Dashboard.OpsGamedataManage,
            TagGamedata
        );
        MapReadGet(
            app,
            ApiGamedata + "/entries",
            (HttpContext ctx, DashboardApiService api) =>
                Results.Ok(api.GamedataEntries(ctx.QueryAsNameValues())),
            Capabilities.Dashboard.OpsGamedataManage,
            TagGamedata
        );
        MapReadGet(
            app,
            ApiGamedata + "/languages",
            (DashboardApiService api) => Results.Ok(api.GamedataLanguages()),
            Capabilities.Dashboard.OpsGamedataManage,
            TagGamedata
        );
    }

    public static void MapGamedataOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/gamedata/entry",
            async (
                HttpContext ctx,
                GamedataEntryRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.File) || string.IsNullOrWhiteSpace(body.Key)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.SaveGamedataEntryAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsGamedataManage,
            TagGamedata
        );
        MapPost(
            app,
            ApiOperations + "/gamedata/entry/delete",
            async (
                HttpContext ctx,
                GamedataEntryDeleteRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.File) || string.IsNullOrWhiteSpace(body.Key)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeleteGamedataEntryAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsGamedataManage,
            TagGamedata
        );
        MapPost(
            app,
            ApiOperations + "/gamedata/furni",
            async (
                HttpContext ctx,
                GamedataFurniRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.Kind) || string.IsNullOrWhiteSpace(body.Field)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.SaveGamedataFurniAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsGamedataManage,
            TagGamedata
        );
        MapPost(
            app,
            ApiOperations + "/gamedata/language",
            async (
                HttpContext ctx,
                GamedataLanguageRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.Code)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.EnableGamedataLanguageAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsGamedataManage,
            TagGamedata
        );
        MapPost(
            app,
            ApiOperations + "/gamedata/language/delete",
            async (
                HttpContext ctx,
                GamedataLanguageRemoveRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.Code)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DisableGamedataLanguageAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsGamedataManage,
            TagGamedata
        );
    }
}
