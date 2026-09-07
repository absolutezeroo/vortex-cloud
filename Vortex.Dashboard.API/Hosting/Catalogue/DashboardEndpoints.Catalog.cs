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
using Vortex.Dashboard.API.Api.Catalogue;
using Vortex.Dashboard.API.Api.Catalogue.Contracts;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Dashboard.API.Operations;
using Vortex.Dashboard.API.Operations.Catalogue;
using Vortex.Dashboard.API.Operations.Catalogue.Contracts;
using Vortex.Dashboard.API.Security;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

internal static partial class DashboardEndpoints
{
    public static void MapCatalogReads(WebApplication app)
    {
        MapReadGet<CatalogPageList>(
            app,
            ApiCatalog + "/pages",
            (HttpContext ctx, CatalogReads api, CancellationToken ct) =>
                OkAsync(api.CatalogPagesAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.CatalogRead,
            TagCatalog
        );
        MapReadGetNullable<CatalogPageDetail>(
            app,
            ApiCatalog + "/pages/{pageId:int}",
            (int pageId, CatalogReads api, CancellationToken ct) =>
                OkNullableAsync(api.CatalogPageDetailAsync(pageId, ct)),
            Capabilities.Dashboard.CatalogRead,
            TagCatalog
        );
        MapReadGetNullable<CatalogOfferDetail>(
            app,
            ApiCatalog + "/offers/{offerId:int}",
            (int offerId, CatalogReads api, CancellationToken ct) =>
                OkNullableAsync(api.CatalogOfferDetailAsync(offerId, ct)),
            Capabilities.Dashboard.CatalogRead,
            TagCatalog
        );
        MapReadGet<CatalogCurrencyList>(
            app,
            ApiCatalog + "/currency-types",
            (CatalogReads api, CancellationToken ct) => OkAsync(api.CatalogCurrencyTypesAsync(ct)),
            Capabilities.Dashboard.CatalogRead,
            TagCatalog
        );
        MapReadGet<CatalogIconTemplate>(
            app,
            ApiCatalog + "/icon-template",
            (CatalogReads api) => Results.Ok(api.CatalogIconTemplate()),
            Capabilities.Dashboard.CatalogRead,
            TagCatalog
        );
    }

    public static void MapCatalogOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/catalog/pages",
            async (
                HttpContext ctx,
                CreateCatalogPageRequest body,
                CatalogOperations ops,
                CancellationToken ct
            ) =>
            {
                if (
                    string.IsNullOrWhiteSpace(body.Localization)
                    || string.IsNullOrWhiteSpace(body.Layout)
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
            async (
                HttpContext ctx,
                UpdateCatalogPageRequest body,
                CatalogOperations ops,
                CancellationToken ct
            ) =>
            {
                if (
                    body.PageId <= 0
                    || string.IsNullOrWhiteSpace(body.Localization)
                    || string.IsNullOrWhiteSpace(body.Layout)
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
            async (
                HttpContext ctx,
                DeleteCatalogPageRequest body,
                CatalogOperations ops,
                CancellationToken ct
            ) =>
            {
                if (body.PageId <= 0)
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
            async (
                HttpContext ctx,
                CreateCatalogOfferRequest body,
                CatalogOperations ops,
                CancellationToken ct
            ) =>
            {
                if (body.PageId <= 0 || string.IsNullOrWhiteSpace(body.LocalizationId))
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
            async (
                HttpContext ctx,
                UpdateCatalogOfferRequest body,
                CatalogOperations ops,
                CancellationToken ct
            ) =>
            {
                if (body.OfferId <= 0 || string.IsNullOrWhiteSpace(body.LocalizationId))
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
            async (
                HttpContext ctx,
                DeleteCatalogOfferRequest body,
                CatalogOperations ops,
                CancellationToken ct
            ) =>
            {
                if (body.OfferId <= 0)
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
            async (
                HttpContext ctx,
                CreateCatalogProductRequest body,
                CatalogOperations ops,
                CancellationToken ct
            ) =>
            {
                if (body.OfferId <= 0)
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
            async (
                HttpContext ctx,
                UpdateCatalogProductRequest body,
                CatalogOperations ops,
                CancellationToken ct
            ) =>
            {
                if (body.ProductId <= 0)
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
            async (
                HttpContext ctx,
                DeleteCatalogProductRequest body,
                CatalogOperations ops,
                CancellationToken ct
            ) =>
            {
                if (body.ProductId <= 0)
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
