using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Dashboard.API.Operations;
using Vortex.Dashboard.API.Security;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

internal static partial class DashboardEndpoints
{
    public static void MapEconomyReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiEconomy + "/ledger",
            "/api/economy",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.EconomyAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.EconomyRead,
            TagEconomy
        );
        MapReadGet(
            app,
            ApiEconomy + "/subscriptions",
            "/api/economy/subscriptions",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.ClubSubscriptionsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.EconomyRead,
            TagEconomy
        );
        MapReadGet(
            app,
            ApiEconomy + "/trends",
            "/api/economy/trends",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.EconomyTrendsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.EconomyRead,
            TagEconomy
        );
        MapReadGet(
            app,
            ApiEconomy + "/marketplace",
            "/api/economy/marketplace",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.MarketplaceSummaryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.EconomyRead,
            TagEconomy
        );
        app.MapGet(
                ApiV1 + "/rentable-spaces/activity",
                (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                    OkAsync(api.RentableSpacesAsync(ctx.QueryAsNameValues(), ct))
            )
            .RequireAuthorization(Capabilities.Dashboard.EconomyRead)
            .WithTags(TagEconomy);
    }

    public static void MapCurrencyOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/currency/credits",
            "/api/ops/currency/credits",
            async (
                HttpContext ctx,
                GiveCreditsRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || body.PlayerId <= 0
                    || body.Amount <= 0
                    || !HasReason(body.Reason)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.GiveCreditsAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsGrantCurrency,
            TagOperations
        );
        MapPost(
            app,
            ApiOperations + "/currency/activity-points",
            "/api/ops/currency/activity-points",
            async (
                HttpContext ctx,
                GiveActivityPointsRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || body.PlayerId <= 0
                    || body.Type < 0
                    || body.Amount <= 0
                    || !HasReason(body.Reason)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.GiveActivityPointsAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsGrantCurrency,
            TagOperations
        );
        MapPost(
            app,
            ApiOperations + "/items/grant",
            "/api/ops/item/grant",
            async (
                HttpContext ctx,
                GiveFurnitureRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || body.PlayerId <= 0
                    || body.DefinitionId <= 0
                    || !HasReason(body.Reason)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.GiveFurnitureAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsGrantItem,
            TagOperations
        );
    }

    public static void MapVoucherOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/vouchers",
            "/api/ops/vouchers",
            async (
                HttpContext ctx,
                CreateVoucherRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || string.IsNullOrWhiteSpace(body.Code)
                    || body.Amount <= 0
                    || body.CurrencyType is < 1 or > 4
                    || !HasReason(body.Reason)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.CreateVoucherAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsManageVouchers,
            TagOperations
        );
        MapPost(
            app,
            ApiOperations + "/vouchers/deactivate",
            "/api/ops/vouchers/deactivate",
            async (
                HttpContext ctx,
                DeactivateVoucherRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body is null || string.IsNullOrWhiteSpace(body.Code) || !HasReason(body.Reason))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.DeactivateVoucherAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsManageVouchers,
            TagOperations
        );
        MapReadGet(
            app,
            ApiOperations + "/vouchers/{code}",
            "/api/ops/vouchers/{code}",
            async (string code, DashboardOperationsService ops, CancellationToken ct) =>
                Results.Ok(await ops.GetVoucherSnapshotAsync(code, ct).ConfigureAwait(false)),
            Capabilities.Dashboard.OpsManageVouchers,
            TagOperations
        );
    }
}
