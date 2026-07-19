using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Operations;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// Targeted-offer admin surface: read (list/detail), purchase analytics (stats), and the CRUD
/// operations that edit an offer live — every write reloads the in-memory offer cache so changes
/// take effect without an emulator restart (see <c>ITargetedOfferAdminService</c>).
/// </summary>
internal static partial class DashboardEndpoints
{
    private const string TagTargetedOffers = "TargetedOffers";
    private const string ApiTargetedOffers = ApiV1 + "/targeted-offers";

    public static void MapTargetedOfferReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiTargetedOffers,
            "/api/targeted-offers",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.TargetedOffersAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.TargetedOffersRead,
            TagTargetedOffers
        );
        MapReadGet(
            app,
            ApiTargetedOffers + "/stats",
            "/api/targeted-offers/stats",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.TargetedOffersStatsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.TargetedOffersRead,
            TagTargetedOffers
        );
        MapReadGet(
            app,
            ApiTargetedOffers + "/{offerId:int}",
            "/api/targeted-offers/{offerId:int}",
            (int offerId, DashboardApiService api, CancellationToken ct) =>
                OkNullableAsync(api.TargetedOfferDetailAsync(offerId, ct)),
            Capabilities.Dashboard.TargetedOffersRead,
            TagTargetedOffers
        );
    }

    public static void MapTargetedOfferOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/targeted-offers",
            "/api/operations/targeted-offers",
            async (
                HttpContext ctx,
                CreateTargetedOfferRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || string.IsNullOrWhiteSpace(body.Identifier)
                    || !HasReason(body.Reason)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.CreateTargetedOfferAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsTargetedOffersManage,
            TagTargetedOffers
        );
        MapPost(
            app,
            ApiOperations + "/targeted-offers/update",
            "/api/operations/targeted-offers/update",
            async (
                HttpContext ctx,
                UpdateTargetedOfferRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || body.OfferId <= 0
                    || string.IsNullOrWhiteSpace(body.Identifier)
                    || !HasReason(body.Reason)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.UpdateTargetedOfferAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsTargetedOffersManage,
            TagTargetedOffers
        );
        MapPost(
            app,
            ApiOperations + "/targeted-offers/delete",
            "/api/operations/targeted-offers/delete",
            async (
                HttpContext ctx,
                DeleteTargetedOfferRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body is null || body.OfferId <= 0 || !HasReason(body.Reason))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.DeleteTargetedOfferAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsTargetedOffersManage,
            TagTargetedOffers
        );
        MapPost(
            app,
            ApiOperations + "/targeted-offers/products",
            "/api/operations/targeted-offers/products",
            async (
                HttpContext ctx,
                CreateTargetedOfferProductRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || body.OfferId <= 0
                    || string.IsNullOrWhiteSpace(body.ProductCode)
                    || !HasReason(body.Reason)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.CreateTargetedOfferProductAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsTargetedOffersManage,
            TagTargetedOffers
        );
        MapPost(
            app,
            ApiOperations + "/targeted-offers/products/update",
            "/api/operations/targeted-offers/products/update",
            async (
                HttpContext ctx,
                UpdateTargetedOfferProductRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || body.ProductId <= 0
                    || string.IsNullOrWhiteSpace(body.ProductCode)
                    || !HasReason(body.Reason)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.UpdateTargetedOfferProductAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsTargetedOffersManage,
            TagTargetedOffers
        );
        MapPost(
            app,
            ApiOperations + "/targeted-offers/products/delete",
            "/api/operations/targeted-offers/products/delete",
            async (
                HttpContext ctx,
                DeleteTargetedOfferProductRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body is null || body.ProductId <= 0 || !HasReason(body.Reason))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.DeleteTargetedOfferProductAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsTargetedOffersManage,
            TagTargetedOffers
        );
    }
}
