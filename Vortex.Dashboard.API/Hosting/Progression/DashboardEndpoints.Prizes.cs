using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Operations;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

internal static partial class DashboardEndpoints
{
    private const string TagPrizePools = "PrizePools";
    private const string ApiPrizePools = ApiV1 + "/prize-pools";

    public static void MapPrizePoolReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiPrizePools,
            (PrizePoolReads reads, CancellationToken ct) => OkAsync(reads.PrizePoolsAsync(ct)),
            Capabilities.Dashboard.PrizePoolsRead,
            TagPrizePools
        );
        MapReadGet(
            app,
            ApiPrizePools + "/stats",
            (HttpContext ctx, PrizePoolReads reads, CancellationToken ct) =>
                OkAsync(reads.PrizePoolStatsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.PrizePoolsRead,
            TagPrizePools
        );
    }

    public static void MapPrizePoolOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/prize-pools",
            async (
                HttpContext ctx,
                CreatePrizePoolRequest body,
                PrizePoolOperations ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.Code) || string.IsNullOrWhiteSpace(body.Name)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreatePrizePoolAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsPrizePoolsManage,
            TagPrizePools
        );
        MapPost(
            app,
            ApiOperations + "/prize-pools/update",
            async (
                HttpContext ctx,
                UpdatePrizePoolRequest body,
                PrizePoolOperations ops,
                CancellationToken ct
            ) =>
                body.PoolId <= 0
                || string.IsNullOrWhiteSpace(body.Code)
                || string.IsNullOrWhiteSpace(body.Name)
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdatePrizePoolAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsPrizePoolsManage,
            TagPrizePools
        );
        MapPost(
            app,
            ApiOperations + "/prize-pools/delete",
            async (
                HttpContext ctx,
                DeletePrizePoolRequest body,
                PrizePoolOperations ops,
                CancellationToken ct
            ) =>
                body.PoolId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeletePrizePoolAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsPrizePoolsManage,
            TagPrizePools
        );
        MapPost(
            app,
            ApiOperations + "/prize-pools/entries",
            async (
                HttpContext ctx,
                CreatePrizeEntryRequest body,
                PrizePoolOperations ops,
                CancellationToken ct
            ) =>
                string.IsNullOrWhiteSpace(body.PoolCode)
                || string.IsNullOrWhiteSpace(body.ProductType)
                || body.Weight <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreatePrizeEntryAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsPrizePoolsManage,
            TagPrizePools
        );
        MapPost(
            app,
            ApiOperations + "/prize-pools/entries/update",
            async (
                HttpContext ctx,
                UpdatePrizeEntryRequest body,
                PrizePoolOperations ops,
                CancellationToken ct
            ) =>
                body.EntryId <= 0
                || string.IsNullOrWhiteSpace(body.PoolCode)
                || string.IsNullOrWhiteSpace(body.ProductType)
                || body.Weight <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdatePrizeEntryAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsPrizePoolsManage,
            TagPrizePools
        );
        MapPost(
            app,
            ApiOperations + "/prize-pools/entries/delete",
            async (
                HttpContext ctx,
                DeletePrizeEntryRequest body,
                PrizePoolOperations ops,
                CancellationToken ct
            ) =>
                body.EntryId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeletePrizeEntryAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsPrizePoolsManage,
            TagPrizePools
        );
        MapPost(
            app,
            ApiOperations + "/prize-pools/bindings",
            async (
                HttpContext ctx,
                CreatePrizeBindingRequest body,
                PrizePoolOperations ops,
                CancellationToken ct
            ) =>
                body.FurnitureDefinitionId <= 0
                || string.IsNullOrWhiteSpace(body.PoolCode)
                || body.HitsRequired <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.CreatePrizeBindingAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsPrizePoolsManage,
            TagPrizePools
        );
        MapPost(
            app,
            ApiOperations + "/prize-pools/bindings/update",
            async (
                HttpContext ctx,
                UpdatePrizeBindingRequest body,
                PrizePoolOperations ops,
                CancellationToken ct
            ) =>
                body.BindingId <= 0
                || body.FurnitureDefinitionId <= 0
                || string.IsNullOrWhiteSpace(body.PoolCode)
                || body.HitsRequired <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.UpdatePrizeBindingAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsPrizePoolsManage,
            TagPrizePools
        );
        MapPost(
            app,
            ApiOperations + "/prize-pools/bindings/delete",
            async (
                HttpContext ctx,
                DeletePrizeBindingRequest body,
                PrizePoolOperations ops,
                CancellationToken ct
            ) =>
                body.BindingId <= 0
                    ? Results.BadRequest(new { error = "invalid_request" })
                    : Results.Ok(
                        await ops.DeletePrizeBindingAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    ),
            Capabilities.Dashboard.OpsPrizePoolsManage,
            TagPrizePools
        );
        MapPost(
            app,
            ApiOperations + "/prize-pools/reload",
            async (
                HttpContext ctx,
                ReloadPrizePoolsRequest body,
                PrizePoolOperations ops,
                CancellationToken ct
            ) =>
                Results.Ok(
                    await ops.ReloadPrizePoolsAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                ),
            Capabilities.Dashboard.OpsPrizePoolsManage,
            TagPrizePools
        );
    }
}
