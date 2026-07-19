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
    public static void MapCatalogReads(WebApplication app)
    {
        MapReadGet(
            app,
            ApiCatalog + "/pages",
            "/api/catalog/pages",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.CatalogPagesAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.CatalogRead,
            TagCatalog
        );
        MapReadGet(
            app,
            ApiCatalog + "/pages/{pageId:int}",
            "/api/catalog/pages/{pageId:int}",
            (int pageId, DashboardApiService api, CancellationToken ct) =>
                OkNullableAsync(api.CatalogPageDetailAsync(pageId, ct)),
            Capabilities.Dashboard.CatalogRead,
            TagCatalog
        );
        MapReadGet(
            app,
            ApiCatalog + "/offers/{offerId:int}",
            "/api/catalog/offers/{offerId:int}",
            (int offerId, DashboardApiService api, CancellationToken ct) =>
                OkNullableAsync(api.CatalogOfferDetailAsync(offerId, ct)),
            Capabilities.Dashboard.CatalogRead,
            TagCatalog
        );
        MapReadGet(
            app,
            ApiCatalog + "/currency-types",
            "/api/catalog/currency-types",
            (DashboardApiService api, CancellationToken ct) =>
                OkAsync(api.CatalogCurrencyTypesAsync(ct)),
            Capabilities.Dashboard.CatalogRead,
            TagCatalog
        );
        MapReadGet(
            app,
            ApiCatalog + "/icon-template",
            "/api/catalog/icon-template",
            (DashboardApiService api) => Results.Ok(api.CatalogIconTemplate()),
            Capabilities.Dashboard.CatalogRead,
            TagCatalog
        );
    }

    public static void MapCatalogOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/catalog/pages",
            "/api/ops/catalog/pages",
            async (
                HttpContext ctx,
                CreateCatalogPageRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || string.IsNullOrWhiteSpace(body.Localization)
                    || string.IsNullOrWhiteSpace(body.Layout)
                    || !HasReason(body.Reason)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.CreateCatalogPageAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsCatalogManage,
            TagCatalog
        );
        MapPost(
            app,
            ApiOperations + "/catalog/pages/update",
            "/api/ops/catalog/pages/update",
            async (
                HttpContext ctx,
                UpdateCatalogPageRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || body.PageId <= 0
                    || string.IsNullOrWhiteSpace(body.Localization)
                    || string.IsNullOrWhiteSpace(body.Layout)
                    || !HasReason(body.Reason)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.UpdateCatalogPageAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsCatalogManage,
            TagCatalog
        );
        MapPost(
            app,
            ApiOperations + "/catalog/pages/delete",
            "/api/ops/catalog/pages/delete",
            async (
                HttpContext ctx,
                DeleteCatalogPageRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body is null || body.PageId <= 0 || !HasReason(body.Reason))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.DeleteCatalogPageAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsCatalogManage,
            TagCatalog
        );

        MapPost(
            app,
            ApiOperations + "/catalog/offers",
            "/api/ops/catalog/offers",
            async (
                HttpContext ctx,
                CreateCatalogOfferRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || body.PageId <= 0
                    || string.IsNullOrWhiteSpace(body.LocalizationId)
                    || !HasReason(body.Reason)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.CreateCatalogOfferAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsCatalogManage,
            TagCatalog
        );
        MapPost(
            app,
            ApiOperations + "/catalog/offers/update",
            "/api/ops/catalog/offers/update",
            async (
                HttpContext ctx,
                UpdateCatalogOfferRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body is null
                    || body.OfferId <= 0
                    || string.IsNullOrWhiteSpace(body.LocalizationId)
                    || !HasReason(body.Reason)
                )
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.UpdateCatalogOfferAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsCatalogManage,
            TagCatalog
        );
        MapPost(
            app,
            ApiOperations + "/catalog/offers/delete",
            "/api/ops/catalog/offers/delete",
            async (
                HttpContext ctx,
                DeleteCatalogOfferRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body is null || body.OfferId <= 0 || !HasReason(body.Reason))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.DeleteCatalogOfferAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsCatalogManage,
            TagCatalog
        );

        MapPost(
            app,
            ApiOperations + "/catalog/products",
            "/api/ops/catalog/products",
            async (
                HttpContext ctx,
                CreateCatalogProductRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body is null || body.OfferId <= 0 || !HasReason(body.Reason))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.CreateCatalogProductAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsCatalogManage,
            TagCatalog
        );
        MapPost(
            app,
            ApiOperations + "/catalog/products/update",
            "/api/ops/catalog/products/update",
            async (
                HttpContext ctx,
                UpdateCatalogProductRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body is null || body.ProductId <= 0 || !HasReason(body.Reason))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.UpdateCatalogProductAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsCatalogManage,
            TagCatalog
        );
        MapPost(
            app,
            ApiOperations + "/catalog/products/delete",
            "/api/ops/catalog/products/delete",
            async (
                HttpContext ctx,
                DeleteCatalogProductRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body is null || body.ProductId <= 0 || !HasReason(body.Reason))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.DeleteCatalogProductAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsCatalogManage,
            TagCatalog
        );
    }
}
