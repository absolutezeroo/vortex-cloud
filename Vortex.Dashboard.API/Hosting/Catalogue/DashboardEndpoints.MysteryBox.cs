using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Operations;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// Mystery box admin surface: read (box colours, prize pools, activity), and the CRUD that edits the
/// pools live — every write reloads the in-memory cache so a colour, a prize or a weight takes effect
/// without an emulator restart (see <c>IMysteryBoxAdminService</c>). Key grants live here too: keys
/// are not furniture, so this and the console command are the only routes into a player's keys.
/// </summary>
internal static partial class DashboardEndpoints
{
    private const string TagMysteryBox = "MysteryBox";
    private const string ApiMysteryBox = ApiV1 + "/mystery-box";

    public static void MapMysteryBoxReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiMysteryBox,
            (DashboardApiService api, CancellationToken ct) => OkAsync(api.MysteryBoxAsync(ct)),
            Capabilities.Dashboard.MysteryBoxRead,
            TagMysteryBox
        );
        MapReadGet(
            app,
            ApiMysteryBox + "/stats",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.MysteryBoxStatsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.MysteryBoxRead,
            TagMysteryBox
        );
        MapReadGet(
            app,
            ApiMysteryBox + "/colors",
            (DashboardApiService api) => Results.Ok(api.MysteryBoxColorOptions()),
            Capabilities.Dashboard.MysteryBoxRead,
            TagMysteryBox
        );
    }

    public static void MapMysteryBoxOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/mystery-box/prizes",
            async (
                HttpContext ctx,
                CreateMysteryBoxPrizeRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    string.IsNullOrWhiteSpace(body.Pool)
                    || string.IsNullOrWhiteSpace(body.ProductType)
                    || body.Weight <= 0
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.CreateMysteryBoxPrizeAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsMysteryBoxManage,
            TagMysteryBox
        );
        MapPost(
            app,
            ApiOperations + "/mystery-box/prizes/update",
            async (
                HttpContext ctx,
                UpdateMysteryBoxPrizeRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body.PrizeId <= 0
                    || string.IsNullOrWhiteSpace(body.Pool)
                    || string.IsNullOrWhiteSpace(body.ProductType)
                    || body.Weight <= 0
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.UpdateMysteryBoxPrizeAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsMysteryBoxManage,
            TagMysteryBox
        );
        MapPost(
            app,
            ApiOperations + "/mystery-box/prizes/delete",
            async (
                HttpContext ctx,
                DeleteMysteryBoxPrizeRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body.PrizeId <= 0)
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.DeleteMysteryBoxPrizeAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsMysteryBoxManage,
            TagMysteryBox
        );
        MapPost(
            app,
            ApiOperations + "/mystery-box/keys",
            async (
                HttpContext ctx,
                GrantMysteryBoxKeyRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body.PlayerId <= 0 || string.IsNullOrWhiteSpace(body.Color))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.GrantMysteryBoxKeyAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsMysteryBoxManage,
            TagMysteryBox
        );
        MapPost(
            app,
            ApiOperations + "/mystery-box/boxes",
            async (
                HttpContext ctx,
                GrantMysteryBoxRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body.PlayerId <= 0
                    || body.FurnitureDefinitionId <= 0
                    || string.IsNullOrWhiteSpace(body.Color)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.GrantMysteryBoxAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsMysteryBoxManage,
            TagMysteryBox
        );
        MapPost(
            app,
            ApiOperations + "/mystery-box/reload",
            async (
                HttpContext ctx,
                ReloadMysteryBoxRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                return Results.Ok(
                    await ops.ReloadMysteryBoxAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsMysteryBoxManage,
            TagMysteryBox
        );
    }
}
