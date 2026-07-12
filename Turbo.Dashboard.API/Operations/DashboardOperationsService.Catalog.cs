using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Observability.Diagnostics;
using Turbo.Primitives.Action;
using Turbo.Primitives.Catalog;
using Turbo.Primitives.Catalog.Admin;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Moderation;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Observability;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Enums.Wallet;
using Turbo.Primitives.Rooms;
using Turbo.Primitives.Rooms.Snapshots.Avatars;

namespace Turbo.Dashboard.API.Operations;

internal sealed partial class DashboardOperationsService
{
    public Task<OperationResult> CreateCatalogPageAsync(
        CreateCatalogPageRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.catalog.page.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.CatalogType,
                request.ParentId,
                request.Localization,
                request.Layout,
            },
            work: async c =>
            {
                CatalogAdminResult result = await _catalogAdmin
                    .CreatePageAsync(
                        new CatalogPageCreateSpec(
                            request.CatalogType,
                            request.ParentId,
                            request.Localization,
                            request.Name,
                            request.Icon,
                            CatalogPageLayoutExtensions.FromLayoutString(request.Layout),
                            request.ImageData,
                            request.TextData,
                            request.SortOrder,
                            request.Visible
                        ),
                        c
                    )
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<OperationResult> UpdateCatalogPageAsync(
        UpdateCatalogPageRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.catalog.page.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.PageId,
                request.ParentId,
                request.Localization,
                request.Layout,
            },
            work: async c =>
            {
                CatalogAdminResult result = await _catalogAdmin
                    .UpdatePageAsync(
                        request.PageId,
                        new CatalogPageUpdateSpec(
                            request.ParentId,
                            request.Localization,
                            request.Name,
                            request.Icon,
                            CatalogPageLayoutExtensions.FromLayoutString(request.Layout),
                            request.ImageData,
                            request.TextData,
                            request.SortOrder,
                            request.Visible
                        ),
                        c
                    )
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<OperationResult> DeleteCatalogPageAsync(
        DeleteCatalogPageRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.catalog.page.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.PageId },
            work: async c =>
            {
                CatalogAdminResult result = await _catalogAdmin
                    .DeletePageAsync(request.PageId, c)
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<OperationResult> CreateCatalogOfferAsync(
        CreateCatalogOfferRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.catalog.offer.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.PageId,
                request.LocalizationId,
                request.CostCredits,
                request.CostCurrency,
            },
            work: async c =>
            {
                CatalogAdminResult result = await _catalogAdmin
                    .CreateOfferAsync(
                        new CatalogOfferCreateSpec(
                            request.PageId,
                            request.LocalizationId,
                            request.CostCredits,
                            request.CostCurrency,
                            request.CurrencyTypeId,
                            request.CanGift,
                            request.CanBundle,
                            request.ClubLevel,
                            request.DiscountPercent,
                            request.Visible
                        ),
                        c
                    )
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<OperationResult> UpdateCatalogOfferAsync(
        UpdateCatalogOfferRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.catalog.offer.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.OfferId,
                request.LocalizationId,
                request.CostCredits,
                request.CostCurrency,
            },
            work: async c =>
            {
                CatalogAdminResult result = await _catalogAdmin
                    .UpdateOfferAsync(
                        request.OfferId,
                        new CatalogOfferUpdateSpec(
                            request.LocalizationId,
                            request.CostCredits,
                            request.CostCurrency,
                            request.CurrencyTypeId,
                            request.CanGift,
                            request.CanBundle,
                            request.ClubLevel,
                            request.DiscountPercent,
                            request.Visible
                        ),
                        c
                    )
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<OperationResult> DeleteCatalogOfferAsync(
        DeleteCatalogOfferRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.catalog.offer.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.OfferId },
            work: async c =>
            {
                CatalogAdminResult result = await _catalogAdmin
                    .DeleteOfferAsync(request.OfferId, c)
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<OperationResult> CreateCatalogProductAsync(
        CreateCatalogProductRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.catalog.product.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.OfferId,
                request.ProductType,
                request.FurnitureDefinitionId,
                request.Quantity,
            },
            work: async c =>
            {
                CatalogAdminResult result = await _catalogAdmin
                    .CreateProductAsync(
                        new CatalogProductCreateSpec(
                            request.OfferId,
                            request.ProductType,
                            request.FurnitureDefinitionId,
                            request.ExtraParam,
                            request.Quantity,
                            request.UniqueSize,
                            request.UniqueRemaining,
                            request.BuildersClubEligible
                        ),
                        c
                    )
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<OperationResult> UpdateCatalogProductAsync(
        UpdateCatalogProductRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.catalog.product.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.ProductId,
                request.ProductType,
                request.FurnitureDefinitionId,
                request.Quantity,
            },
            work: async c =>
            {
                CatalogAdminResult result = await _catalogAdmin
                    .UpdateProductAsync(
                        request.ProductId,
                        new CatalogProductUpdateSpec(
                            request.ProductType,
                            request.FurnitureDefinitionId,
                            request.ExtraParam,
                            request.Quantity,
                            request.UniqueSize,
                            request.UniqueRemaining,
                            request.BuildersClubEligible
                        ),
                        c
                    )
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<OperationResult> DeleteCatalogProductAsync(
        DeleteCatalogProductRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.catalog.product.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.ProductId },
            work: async c =>
            {
                CatalogAdminResult result = await _catalogAdmin
                    .DeleteProductAsync(request.ProductId, c)
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );
}
