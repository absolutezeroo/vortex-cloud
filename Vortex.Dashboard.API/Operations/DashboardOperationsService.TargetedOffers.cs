using System;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Catalog.Admin;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Targeted-offer admin operations. Each routes through <see cref="ITargetedOfferAdminService"/>
/// (never a direct DB write), which reloads the live offer cache after committing, and emits a
/// durable audit event with the operator's reason — same contract as the catalog operations.
/// </summary>
internal sealed partial class DashboardOperationsService
{
    public Task<OperationResult> CreateTargetedOfferAsync(
        CreateTargetedOfferRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.targeted_offer.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.Identifier,
                request.PriceInCredits,
                request.PriceInActivityPoints,
                request.PurchaseLimit,
            },
            work: async c =>
            {
                CatalogAdminResult result = await _targetedOfferAdmin
                    .CreateOfferAsync(
                        new TargetedOfferCreateSpec(
                            request.Identifier,
                            request.OfferType,
                            request.Title,
                            request.Description,
                            request.ImageUrl,
                            request.IconImageUrl,
                            request.ProductCode,
                            request.PriceInCredits,
                            request.PriceInActivityPoints,
                            request.ActivityPointType,
                            request.PurchaseLimit,
                            request.ExpiresAt,
                            request.Active,
                            request.SortOrder
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

    public Task<OperationResult> UpdateTargetedOfferAsync(
        UpdateTargetedOfferRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.targeted_offer.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.OfferId,
                request.Identifier,
                request.PriceInCredits,
                request.Active,
            },
            work: async c =>
            {
                CatalogAdminResult result = await _targetedOfferAdmin
                    .UpdateOfferAsync(
                        request.OfferId,
                        new TargetedOfferUpdateSpec(
                            request.Identifier,
                            request.OfferType,
                            request.Title,
                            request.Description,
                            request.ImageUrl,
                            request.IconImageUrl,
                            request.ProductCode,
                            request.PriceInCredits,
                            request.PriceInActivityPoints,
                            request.ActivityPointType,
                            request.PurchaseLimit,
                            request.ExpiresAt,
                            request.Active,
                            request.SortOrder
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

    public Task<OperationResult> DeleteTargetedOfferAsync(
        DeleteTargetedOfferRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.targeted_offer.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.OfferId },
            work: async c =>
            {
                CatalogAdminResult result = await _targetedOfferAdmin
                    .DeleteOfferAsync(request.OfferId, c)
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<OperationResult> CreateTargetedOfferProductAsync(
        CreateTargetedOfferProductRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.targeted_offer.product.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.OfferId,
                request.ProductCode,
                request.FurnitureDefinitionId,
                request.Quantity,
            },
            work: async c =>
            {
                CatalogAdminResult result = await _targetedOfferAdmin
                    .CreateProductAsync(
                        new TargetedOfferProductCreateSpec(
                            request.OfferId,
                            request.ProductCode,
                            request.FurnitureDefinitionId,
                            request.Quantity
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

    public Task<OperationResult> UpdateTargetedOfferProductAsync(
        UpdateTargetedOfferProductRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.targeted_offer.product.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.ProductId,
                request.ProductCode,
                request.FurnitureDefinitionId,
                request.Quantity,
            },
            work: async c =>
            {
                CatalogAdminResult result = await _targetedOfferAdmin
                    .UpdateProductAsync(
                        request.ProductId,
                        new TargetedOfferProductUpdateSpec(
                            request.ProductCode,
                            request.FurnitureDefinitionId,
                            request.Quantity
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

    public Task<OperationResult> DeleteTargetedOfferProductAsync(
        DeleteTargetedOfferProductRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.targeted_offer.product.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.ProductId },
            work: async c =>
            {
                CatalogAdminResult result = await _targetedOfferAdmin
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
