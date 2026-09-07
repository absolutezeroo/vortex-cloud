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
using Vortex.Primitives.Players.Enums.Wallet;

namespace Vortex.Dashboard.API.Hosting;

internal static partial class DashboardEndpoints
{
    public static void MapEconomyReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiEconomy + "/ledger",
            (HttpContext ctx, EconomyReads reads, CancellationToken ct) =>
                OkAsync(reads.EconomyAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.EconomyRead,
            TagEconomy
        );
        MapReadGet(
            app,
            ApiEconomy + "/subscriptions",
            (HttpContext ctx, EconomyReads reads, CancellationToken ct) =>
                OkAsync(reads.ClubSubscriptionsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.EconomyRead,
            TagEconomy
        );
        MapReadGet(
            app,
            ApiEconomy + "/trends",
            (HttpContext ctx, EconomyReads reads, CancellationToken ct) =>
                OkAsync(reads.EconomyTrendsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.EconomyRead,
            TagEconomy
        );
        MapReadGet(
            app,
            ApiEconomy + "/marketplace",
            (HttpContext ctx, EconomyReads reads, CancellationToken ct) =>
                OkAsync(reads.MarketplaceSummaryAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.EconomyRead,
            TagEconomy
        );
        app.MapGet(
                ApiV1 + "/rentable-spaces/activity",
                (HttpContext ctx, EconomyReads reads, CancellationToken ct) =>
                    OkAsync(reads.RentableSpacesAsync(ctx.QueryAsNameValues(), ct))
            )
            .RequireAuthorization(Capabilities.Dashboard.EconomyRead)
            .WithTags(TagEconomy);
    }

    public static void MapCurrencyOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/currency/credits",
            async (
                HttpContext ctx,
                GiveCreditsRequest body,
                CurrencyOperations currencyOps,
                CancellationToken ct
            ) =>
            {
                if (body.PlayerId <= 0 || body.Amount <= 0)
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await currencyOps
                        .GiveCreditsAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsGrantCurrency,
            TagOperations
        );
        MapPost(
            app,
            ApiOperations + "/currency/activity-points",
            async (
                HttpContext ctx,
                GiveActivityPointsRequest body,
                CurrencyOperations currencyOps,
                CancellationToken ct
            ) =>
            {
                if (body.PlayerId <= 0 || body.Type < 0 || body.Amount <= 0)
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await currencyOps
                        .GiveActivityPointsAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsGrantCurrency,
            TagOperations
        );
        MapPost(
            app,
            ApiOperations + "/currency/collectibles",
            async (
                HttpContext ctx,
                GiveCollectiblesCurrencyRequest body,
                CurrencyOperations currencyOps,
                CancellationToken ct
            ) =>
            {
                if (
                    body.PlayerId <= 0
                    || body.Amount <= 0
                    || !CurrencyOperations.TryParseCollectiblesCurrency(
                        body.Currency,
                        out CurrencyType currency
                    )
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await currencyOps
                        .GiveCollectiblesCurrencyAsync(body, currency, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsGrantCurrency,
            TagOperations
        );
        MapPost(
            app,
            ApiOperations + "/items/grant",
            async (
                HttpContext ctx,
                GiveFurnitureRequest body,
                CurrencyOperations currencyOps,
                CancellationToken ct
            ) =>
            {
                if (body.PlayerId <= 0 || body.DefinitionId <= 0)
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await currencyOps
                        .GiveFurnitureAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
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
            async (
                HttpContext ctx,
                CreateVoucherRequest body,
                VouchersOperations vouchers,
                CancellationToken ct
            ) =>
            {
                if (
                    string.IsNullOrWhiteSpace(body.Code)
                    || body.Amount <= 0
                    || body.CurrencyType is < 1 or > 4
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await vouchers
                        .CreateVoucherAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsManageVouchers,
            TagOperations
        );
        MapPost(
            app,
            ApiOperations + "/vouchers/deactivate",
            async (
                HttpContext ctx,
                DeactivateVoucherRequest body,
                VouchersOperations vouchers,
                CancellationToken ct
            ) =>
            {
                if (string.IsNullOrWhiteSpace(body.Code))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await vouchers
                        .DeactivateVoucherAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsManageVouchers,
            TagOperations
        );
        MapReadGet(
            app,
            ApiOperations + "/vouchers/{code}",
            async (string code, VouchersOperations vouchers, CancellationToken ct) =>
                Results.Ok(await vouchers.GetVoucherSnapshotAsync(code, ct).ConfigureAwait(false)),
            Capabilities.Dashboard.OpsManageVouchers,
            TagOperations
        );
    }
}
