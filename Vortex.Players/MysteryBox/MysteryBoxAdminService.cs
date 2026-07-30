using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.MysteryBox;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.MysteryBox;
using Vortex.Primitives.MysteryBox.Admin;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;

namespace Vortex.Players.MysteryBox;

/// <summary>
/// CRUD for the mystery box reference tables. A plain singleton (not a grain) opening a short-lived
/// <see cref="VortexDbContext"/> per call: these rows aren't grain-owned and admin writes are
/// low-frequency. The live pools come from the kept-alive <c>MysteryBoxManagerGrain</c> cache, which
/// is only rebuilt via its <c>ReloadAsync</c>, so every write reloads it afterwards — the "DB write
/// not reflected in live state" bug class called out in AGENTS.md.
/// </summary>
internal sealed class MysteryBoxAdminService(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    IGrainFactory grainFactory,
    ILogger<MysteryBoxAdminService> logger
) : IMysteryBoxAdminService
{
    /// <summary>Prize types the client's reward window can draw. Anything else would award silently
    /// into a blank dialog, so it is refused at the admin boundary rather than at draw time.</summary>
    private static readonly ProductType[] DrawableProductTypes =
    [
        ProductType.Floor,
        ProductType.Wall,
        ProductType.Effect,
        ProductType.HabboClub,
    ];

    public async Task<MysteryBoxAdminResult> CreatePrizeAsync(
        MysteryBoxPrizeSpec spec,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        if (await ValidatePrizeAsync(db, spec, ct).ConfigureAwait(false) is { } error)
        {
            return MysteryBoxAdminResult.Fail(error);
        }

        MysteryBoxPrizeEntity entity = new()
        {
            Pool = spec.Pool,
            Color = MysteryBoxColors.Normalize(spec.Color),
            ProductType = spec.ProductType,
            FurnitureDefinitionEntityId = spec.FurnitureDefinitionId,
            ExtraParam = (spec.ExtraParam ?? string.Empty).Trim(),
            Weight = Math.Max(1, spec.Weight),
            Enabled = spec.Enabled,
        };

        db.MysteryBoxPrizes.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return MysteryBoxAdminResult.Ok(entity.Id);
    }

    public async Task<MysteryBoxAdminResult> UpdatePrizeAsync(
        int prizeId,
        MysteryBoxPrizeSpec spec,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        MysteryBoxPrizeEntity? entity = await db
            .MysteryBoxPrizes.FirstOrDefaultAsync(p => p.Id == prizeId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return MysteryBoxAdminResult.Fail("prize_not_found");
        }

        if (await ValidatePrizeAsync(db, spec, ct).ConfigureAwait(false) is { } error)
        {
            return MysteryBoxAdminResult.Fail(error);
        }

        entity.Pool = spec.Pool;
        entity.Color = MysteryBoxColors.Normalize(spec.Color);
        entity.ProductType = spec.ProductType;
        entity.FurnitureDefinitionEntityId = spec.FurnitureDefinitionId;
        entity.ExtraParam = (spec.ExtraParam ?? string.Empty).Trim();
        entity.Weight = Math.Max(1, spec.Weight);
        entity.Enabled = spec.Enabled;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return MysteryBoxAdminResult.Ok(entity.Id);
    }

    public async Task<MysteryBoxAdminResult> DeletePrizeAsync(int prizeId, CancellationToken ct)
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        MysteryBoxPrizeEntity? entity = await db
            .MysteryBoxPrizes.FirstOrDefaultAsync(p => p.Id == prizeId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return MysteryBoxAdminResult.Fail("prize_not_found");
        }

        db.MysteryBoxPrizes.Remove(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadAsync(ct).ConfigureAwait(false);

        return MysteryBoxAdminResult.Ok(prizeId);
    }

    public async Task<MysteryBoxAdminResult> GrantKeyAsync(
        int playerId,
        string color,
        string actor,
        CancellationToken ct
    )
    {
        string normalizedColor = MysteryBoxColors.Normalize(color);

        if (normalizedColor.Length == 0)
        {
            return MysteryBoxAdminResult.Fail("invalid_color");
        }

        if (playerId <= 0)
        {
            return MysteryBoxAdminResult.Fail("player_required");
        }

        // The grain owns the key rows and pushes the player's tracker; writing the row here would
        // leave a connected player's toolbar showing the old colours.
        bool granted = await grainFactory
            .GetPlayerMysteryBoxGrain(playerId)
            .GrantKeyAsync(normalizedColor, $"staff:{actor}", ct)
            .ConfigureAwait(false);

        return granted
            ? MysteryBoxAdminResult.Ok(playerId)
            : MysteryBoxAdminResult.Fail("grant_failed");
    }

    public async Task<MysteryBoxAdminResult> GrantBoxAsync(
        int playerId,
        int furnitureDefinitionId,
        string color,
        string actor,
        CancellationToken ct
    )
    {
        if (playerId <= 0)
        {
            return MysteryBoxAdminResult.Fail("player_required");
        }

        string normalizedColor = MysteryBoxColors.Normalize(color);

        if (normalizedColor.Length == 0)
        {
            return MysteryBoxAdminResult.Fail("invalid_color");
        }

        if (
            !await grainFactory
                .GetMysteryBoxManagerGrain()
                .IsBoxDefinitionAsync(furnitureDefinitionId, ct)
                .ConfigureAwait(false)
        )
        {
            return MysteryBoxAdminResult.Fail("not_a_mystery_box");
        }

        // The colour is the furniture state, so it has to be baked into the item at creation: there
        // is nowhere else for it to live, and a box created at state 0 is colourless and unpairable
        // forever. The inventory grain owns furniture creation and the extra-data shape, and its add
        // path refreshes the recipient's tracker, so the box shows in their toolbar without a relog.
        await grainFactory
            .GetInventoryGrain(playerId)
            .GrantFurnitureWithLegacyStuffDataAsync(
                furnitureDefinitionId,
                MysteryBoxSprite
                    .ClosedState(normalizedColor)
                    .ToString(CultureInfo.InvariantCulture),
                ct
            )
            .ConfigureAwait(false);

        logger.LogInformation(
            "Staff {Actor} granted a {Color} mystery box (definition {DefinitionId}) to player {PlayerId}.",
            actor,
            normalizedColor,
            furnitureDefinitionId,
            playerId
        );

        return MysteryBoxAdminResult.Ok(playerId);
    }

    public async Task<MysteryBoxAdminResult> ReloadCacheAsync(CancellationToken ct)
    {
        await ReloadAsync(ct).ConfigureAwait(false);

        return MysteryBoxAdminResult.Ok(0);
    }

    private static async Task<string?> ValidatePrizeAsync(
        VortexDbContext db,
        MysteryBoxPrizeSpec spec,
        CancellationToken ct
    )
    {
        if (!DrawableProductTypes.Contains(spec.ProductType))
        {
            return "product_type_not_drawable";
        }

        if (spec.Weight <= 0)
        {
            return "weight_must_be_positive";
        }

        // A colour is optional (empty = any), but a typo'd one would silently make the prize
        // undrawable, so reject anything that is neither empty nor renderable.
        if (
            !string.IsNullOrWhiteSpace(spec.Color)
            && MysteryBoxColors.Normalize(spec.Color).Length == 0
        )
        {
            return "invalid_color";
        }

        if (spec.ProductType is ProductType.Floor or ProductType.Wall)
        {
            if (spec.FurnitureDefinitionId <= 0)
            {
                return "furniture_definition_required";
            }

            if (
                !await db
                    .FurnitureDefinitions.AnyAsync(f => f.Id == spec.FurnitureDefinitionId, ct)
                    .ConfigureAwait(false)
            )
            {
                return "furniture_definition_not_found";
            }

            return null;
        }

        // Effect and club prizes carry their target in ExtraParam; an empty one would award nothing
        // and close the winner's reward window on an error.
        if (string.IsNullOrWhiteSpace(spec.ExtraParam))
        {
            return "extra_param_required";
        }

        string head = spec.ExtraParam.Split(':')[0];

        return int.TryParse(head, out int value) && value > 0 ? null : "extra_param_invalid";
    }

    private async Task ReloadAsync(CancellationToken ct)
    {
        try
        {
            await grainFactory.GetMysteryBoxManagerGrain().ReloadAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The DB write already committed -- the live pools are now stale until the next reload or
            // restart. Never swallow this: it is the "DB write not reflected in live state" bug class
            // called out in AGENTS.md.
            logger.LogError(
                ex,
                "Mystery box cache reload failed after an admin write committed -- live definitions and prize pools are now stale until the next reload or restart"
            );
            throw;
        }
    }
}
