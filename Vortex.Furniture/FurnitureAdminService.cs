using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Observability.Diagnostics;
using Vortex.Primitives.Furniture;
using Vortex.Primitives.Furniture.Admin;
using Vortex.Primitives.Furniture.Providers;

namespace Vortex.Furniture;

/// <summary>
/// CRUD for furniture_definitions. Not a grain -- definitions aren't grain-owned and there is no
/// per-row concurrency need. Every write reloads <see cref="IFurnitureDefinitionProvider"/>
/// afterwards so the live in-memory snapshot every handler/serializer reads from never drifts
/// from the database (the same class of bug flagged for the catalog snapshot providers).
/// </summary>
internal sealed class FurnitureAdminService(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    IFurnitureDefinitionProvider definitionProvider,
    ILogger<FurnitureAdminService> logger
) : IFurnitureAdminService
{
    public async Task<FurnitureAdminResult> CreateAsync(
        FurnitureDefinitionUpsertSpec spec,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        bool duplicate = await db
            .FurnitureDefinitions.AsNoTracking()
            .AnyAsync(
                f =>
                    f.SpriteId == spec.SpriteId
                    && f.ProductType == spec.ProductType
                    && f.FurniCategory == spec.FurniCategory,
                ct
            )
            .ConfigureAwait(false);

        if (duplicate)
        {
            return FurnitureAdminResult.Fail("duplicate_sprite_type_category");
        }

        FurnitureDefinitionEntity entity = new()
        {
            SpriteId = spec.SpriteId,
            Name = spec.Name,
            ProductType = spec.ProductType,
            FurniCategory = spec.FurniCategory,
            Logic = spec.Logic,
            TotalStates = spec.TotalStates,
            Width = spec.Width,
            Length = spec.Length,
            StackHeight = spec.StackHeight,
            CanStack = spec.CanStack,
            CanWalk = spec.CanWalk,
            CanSit = spec.CanSit,
            CanLay = spec.CanLay,
            CanRecycle = spec.CanRecycle,
            CanTrade = spec.CanTrade,
            CanGroup = spec.CanGroup,
            CanSell = spec.CanSell,
            UsagePolicy = spec.UsagePolicy,
            ExtraData = spec.ExtraData,
            StuffDataType = spec.StuffDataType,
        };

        db.FurnitureDefinitions.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return FurnitureAdminResult.Ok(entity.Id);
    }

    public async Task<FurnitureAdminResult> UpdateAsync(
        int id,
        FurnitureDefinitionUpsertSpec spec,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        FurnitureDefinitionEntity? entity = await db
            .FurnitureDefinitions.FirstOrDefaultAsync(f => f.Id == id, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return FurnitureAdminResult.Fail("definition_not_found");
        }

        bool duplicate = await db
            .FurnitureDefinitions.AsNoTracking()
            .AnyAsync(
                f =>
                    f.Id != id
                    && f.SpriteId == spec.SpriteId
                    && f.ProductType == spec.ProductType
                    && f.FurniCategory == spec.FurniCategory,
                ct
            )
            .ConfigureAwait(false);

        if (duplicate)
        {
            return FurnitureAdminResult.Fail("duplicate_sprite_type_category");
        }

        entity.SpriteId = spec.SpriteId;
        entity.Name = spec.Name;
        entity.ProductType = spec.ProductType;
        entity.FurniCategory = spec.FurniCategory;
        entity.Logic = spec.Logic;
        entity.TotalStates = spec.TotalStates;
        entity.Width = spec.Width;
        entity.Length = spec.Length;
        entity.StackHeight = spec.StackHeight;
        entity.CanStack = spec.CanStack;
        entity.CanWalk = spec.CanWalk;
        entity.CanSit = spec.CanSit;
        entity.CanLay = spec.CanLay;
        entity.CanRecycle = spec.CanRecycle;
        entity.CanTrade = spec.CanTrade;
        entity.CanGroup = spec.CanGroup;
        entity.CanSell = spec.CanSell;
        entity.UsagePolicy = spec.UsagePolicy;
        entity.ExtraData = spec.ExtraData;
        entity.StuffDataType = spec.StuffDataType;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return FurnitureAdminResult.Ok(entity.Id);
    }

    public async Task<FurnitureAdminResult> DeleteAsync(int id, CancellationToken ct)
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        FurnitureDefinitionEntity? entity = await db
            .FurnitureDefinitions.FirstOrDefaultAsync(f => f.Id == id, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return FurnitureAdminResult.Fail("definition_not_found");
        }

        bool hasInstances = await db
            .Furnitures.AnyAsync(f => f.FurnitureDefinitionEntityId == id, ct)
            .ConfigureAwait(false);

        if (hasInstances)
        {
            return FurnitureAdminResult.Fail("definition_has_instances");
        }

        bool hasCatalogProducts = await db
            .CatalogProducts.AnyAsync(p => p.FurnitureDefinitionEntityId == id, ct)
            .ConfigureAwait(false);

        if (hasCatalogProducts)
        {
            return FurnitureAdminResult.Fail("definition_used_by_catalog_product");
        }

        db.FurnitureDefinitions.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return FurnitureAdminResult.Ok(id);
    }

    private async Task ReloadAsync(CancellationToken ct)
    {
        try
        {
            await definitionProvider.ReloadAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The DB write already committed -- the live snapshot is now stale until the next
            // successful reload or a full restart. Never swallow this.
            logger.LogError(
                VortexEventIds.FurnitureDefinitionReloadFailed,
                ex,
                "Furniture definition snapshot reload failed after an admin write committed -- live definitions are now stale until the next reload or restart"
            );
            throw;
        }
    }
}
