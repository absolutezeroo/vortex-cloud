using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Catalog.Admin;
using Vortex.Primitives.Orleans;

namespace Vortex.Catalog;

/// <summary>
/// CRUD for targeted_offers/targeted_offer_products. Like <see cref="CatalogAdminService"/> this is a
/// plain singleton (not a grain) opening a short-lived <see cref="VortexDbContext"/> per call: these
/// rows aren't grain-owned and admin writes are low-frequency.
///
/// The live offers players see come from the kept-alive <see cref="Grains.ITargetedOfferManagerGrain"/>
/// cache, which is only rebuilt via its <c>ReloadAsync</c>. Every write here reloads that cache
/// afterwards so the DB and the live cache never drift — the same "DB write not reflected in live
/// state" bug class called out in AGENTS.md that <see cref="CatalogAdminService"/> guards against.
/// </summary>
internal sealed class TargetedOfferAdminService(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    IGrainFactory grainFactory,
    ILogger<TargetedOfferAdminService> logger
) : ITargetedOfferAdminService
{
    public async Task<CatalogAdminResult> CreateOfferAsync(
        TargetedOfferCreateSpec spec,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(spec.Identifier))
        {
            return CatalogAdminResult.Fail("identifier_required");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        TargetedOfferEntity entity = new()
        {
            Identifier = spec.Identifier,
            OfferType = spec.OfferType,
            Title = spec.Title,
            Description = spec.Description,
            ImageUrl = spec.ImageUrl,
            IconImageUrl = spec.IconImageUrl,
            ProductCode = spec.ProductCode,
            PriceInCredits = spec.PriceInCredits,
            PriceInActivityPoints = spec.PriceInActivityPoints,
            ActivityPointType = spec.ActivityPointType,
            PurchaseLimit = spec.PurchaseLimit,
            ExpiresAt = spec.ExpiresAt,
            Active = spec.Active,
            SortOrder = spec.SortOrder,
        };

        db.TargetedOffers.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return CatalogAdminResult.Ok(entity.Id);
    }

    public async Task<CatalogAdminResult> UpdateOfferAsync(
        int offerId,
        TargetedOfferUpdateSpec spec,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(spec.Identifier))
        {
            return CatalogAdminResult.Fail("identifier_required");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        TargetedOfferEntity? entity = await db
            .TargetedOffers.FirstOrDefaultAsync(o => o.Id == offerId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return CatalogAdminResult.Fail("offer_not_found");
        }

        entity.Identifier = spec.Identifier;
        entity.OfferType = spec.OfferType;
        entity.Title = spec.Title;
        entity.Description = spec.Description;
        entity.ImageUrl = spec.ImageUrl;
        entity.IconImageUrl = spec.IconImageUrl;
        entity.ProductCode = spec.ProductCode;
        entity.PriceInCredits = spec.PriceInCredits;
        entity.PriceInActivityPoints = spec.PriceInActivityPoints;
        entity.ActivityPointType = spec.ActivityPointType;
        entity.PurchaseLimit = spec.PurchaseLimit;
        entity.ExpiresAt = spec.ExpiresAt;
        entity.Active = spec.Active;
        entity.SortOrder = spec.SortOrder;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return CatalogAdminResult.Ok(entity.Id);
    }

    public async Task<CatalogAdminResult> DeleteOfferAsync(int offerId, CancellationToken ct)
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        TargetedOfferEntity? entity = await db
            .TargetedOffers.FirstOrDefaultAsync(o => o.Id == offerId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return CatalogAdminResult.Fail("offer_not_found");
        }

        // The offer's bundle products cascade-delete (FK Cascade in VortexDbContext), but per-player
        // purchase rows reference the offer with DeleteBehavior.Restrict -- a hard delete would fail
        // at the FK once anyone has bought it, and would also destroy the per-player purchase-limit
        // history. Block it and steer the admin to deactivate (Active=false) instead, mirroring the
        // catalog admin's "has dependents" guards.
        bool hasPurchases = await db
            .PlayerTargetedOffers.AnyAsync(p => p.TargetedOfferEntityId == offerId, ct)
            .ConfigureAwait(false);

        if (hasPurchases)
        {
            return CatalogAdminResult.Fail("offer_has_purchases");
        }

        db.TargetedOffers.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return CatalogAdminResult.Ok(offerId);
    }

    public async Task<CatalogAdminResult> CreateProductAsync(
        TargetedOfferProductCreateSpec spec,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(spec.ProductCode))
        {
            return CatalogAdminResult.Fail("product_code_required");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        bool offerExists = await db
            .TargetedOffers.AsNoTracking()
            .AnyAsync(o => o.Id == spec.OfferId, ct)
            .ConfigureAwait(false);

        if (!offerExists)
        {
            return CatalogAdminResult.Fail("offer_not_found");
        }

        if (spec.FurnitureDefinitionId is { } definitionId)
        {
            bool definitionExists = await db
                .FurnitureDefinitions.AsNoTracking()
                .AnyAsync(f => f.Id == definitionId, ct)
                .ConfigureAwait(false);

            if (!definitionExists)
            {
                return CatalogAdminResult.Fail("furniture_definition_not_found");
            }
        }

        TargetedOfferProductEntity entity = new()
        {
            TargetedOfferEntityId = spec.OfferId,
            ProductCode = spec.ProductCode,
            FurnitureDefinitionEntityId = spec.FurnitureDefinitionId,
            Quantity = Math.Max(1, spec.Quantity),
        };

        db.TargetedOfferProducts.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return CatalogAdminResult.Ok(entity.Id);
    }

    public async Task<CatalogAdminResult> UpdateProductAsync(
        int productId,
        TargetedOfferProductUpdateSpec spec,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(spec.ProductCode))
        {
            return CatalogAdminResult.Fail("product_code_required");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        TargetedOfferProductEntity? entity = await db
            .TargetedOfferProducts.FirstOrDefaultAsync(p => p.Id == productId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return CatalogAdminResult.Fail("product_not_found");
        }

        if (spec.FurnitureDefinitionId is { } definitionId)
        {
            bool definitionExists = await db
                .FurnitureDefinitions.AsNoTracking()
                .AnyAsync(f => f.Id == definitionId, ct)
                .ConfigureAwait(false);

            if (!definitionExists)
            {
                return CatalogAdminResult.Fail("furniture_definition_not_found");
            }
        }

        entity.ProductCode = spec.ProductCode;
        entity.FurnitureDefinitionEntityId = spec.FurnitureDefinitionId;
        entity.Quantity = Math.Max(1, spec.Quantity);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return CatalogAdminResult.Ok(entity.Id);
    }

    public async Task<CatalogAdminResult> DeleteProductAsync(int productId, CancellationToken ct)
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        TargetedOfferProductEntity? entity = await db
            .TargetedOfferProducts.FirstOrDefaultAsync(p => p.Id == productId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return CatalogAdminResult.Fail("product_not_found");
        }

        db.TargetedOfferProducts.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return CatalogAdminResult.Ok(productId);
    }

    private async Task ReloadAsync(CancellationToken ct)
    {
        try
        {
            await grainFactory.GetTargetedOfferManagerGrain().ReloadAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The DB write already committed -- the live offer cache is now stale until the next
            // reload or restart. Never swallow this: it is the "DB write not reflected in live state"
            // bug class called out in AGENTS.md.
            logger.LogError(
                ex,
                "Targeted-offer cache reload failed after an admin write committed -- live offers are now stale until the next reload or restart"
            );
            throw;
        }
    }
}
