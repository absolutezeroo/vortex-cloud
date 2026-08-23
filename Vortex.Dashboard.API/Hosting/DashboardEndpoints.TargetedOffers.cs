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
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.TargetedOffersAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.TargetedOffersRead,
            TagTargetedOffers
        );
        MapReadGet(
            app,
            ApiTargetedOffers + "/stats",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.TargetedOffersStatsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.TargetedOffersRead,
            TagTargetedOffers
        );
        MapReadGet(
            app,
            ApiTargetedOffers + "/form-meta",
            (DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.TargetedOfferFormMetaAsync(ct)),
            Capabilities.Dashboard.TargetedOffersRead,
            TagTargetedOffers
        );
        MapReadGet(
            app,
            ApiTargetedOffers + "/images",
            (DashboardApiService api) => Results.Ok(api.TargetedOfferImages()),
            Capabilities.Dashboard.TargetedOffersRead,
            TagTargetedOffers
        );
        MapReadGet(
            app,
            ApiTargetedOffers + "/{offerId:int}",
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
            async (
                HttpContext ctx,
                CreateTargetedOfferRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (string.IsNullOrWhiteSpace(body.Identifier))
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
            async (
                HttpContext ctx,
                UpdateTargetedOfferRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body.OfferId <= 0 || string.IsNullOrWhiteSpace(body.Identifier))
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
            async (
                HttpContext ctx,
                DeleteTargetedOfferRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body.OfferId <= 0)
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
            async (
                HttpContext ctx,
                CreateTargetedOfferProductRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body.OfferId <= 0 || string.IsNullOrWhiteSpace(body.ProductCode))
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
            async (
                HttpContext ctx,
                UpdateTargetedOfferProductRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body.ProductId <= 0 || string.IsNullOrWhiteSpace(body.ProductCode))
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
            async (
                HttpContext ctx,
                DeleteTargetedOfferProductRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body.ProductId <= 0)
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
