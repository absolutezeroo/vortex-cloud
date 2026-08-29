using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Database.Entities.Pets;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Snapshots.Avatars;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// The monsterplant half of the pet system. A plant is a pet of type 16 that never walks, so almost
/// none of the pet loop applies to it — but the loop it does need is its own: a well-being clock
/// that runs down over a day, growth stages while it stays watered, and death when the clock
/// reaches zero.
/// <para>
/// The numbers here are the client's, not inventions. Its pet-info view counts the well-being down
/// from the <c>MaxWellBeingSeconds</c> the server sends (86 400), its own-pet menu offers "treat"
/// only while <c>energy / energyMax &lt; 0.98</c>, and it offers revive and compost only on a plant
/// whose <c>canRevive</c> is set — which is why well-being <em>is</em> energy here rather than a
/// column of its own.
/// </para>
/// <para>
/// Growth is banked in <c>Experience</c>, which a plant has no other use for and which the stat
/// flush already persists: a plant keeps the hours it has grown across a room reload, where an
/// in-memory counter would quietly restart it.
/// </para>
/// </summary>
public sealed partial class RoomPetSystem
{
    /// <summary>The growth stage a seed opens at. Stage 0 is the seed itself, which is furniture.</summary>
    private const int PlantFirstLevel = 1;

    /// <summary>Full size. The client's own menu compares <c>level == levelMax</c> to decide a plant
    /// is grown, and <c>RoomPetRuntime.CanHarvest</c> uses the same number.</summary>
    private const int PlantMaxLevel = 7;

    /// <summary>
    /// One tick of a monsterplant. Returns the avatar snapshot to broadcast when the plant visibly
    /// changed — a growth stage is part of its figure, so a level-up has to be re-sent or the client
    /// keeps drawing the smaller plant.
    /// </summary>
    private async Task<RoomPetAvatarSnapshot?> ProcessPlantTickAsync(
        PetSnapshot plant,
        PetMotionState motion,
        CancellationToken ct
    )
    {
        int wellBeingSeconds = Math.Max(1, Tuning.PlantWellBeingSeconds);
        int growthSeconds = Math.Max(1, Tuning.PlantGrowthSeconds);
        int energyCap = _roomGrain._petLevelProvider.GetEnergyCapForLevel(plant.Type, plant.Level);
        int maxLevel = PlantMaxLevel;

        DateTime nowUtc = DateTime.UtcNow;
        DateTime wateredAt = plant.LastWateredAt ?? plant.CreatedAt;

        // Well-being is derived from the watering stamp rather than accumulated tick by tick, so it
        // agrees exactly with the countdown the client renders from the same stamp -- and so a room
        // that was asleep for six hours resumes with the right answer instead of a frozen bar.
        int wellBeing = RoomPetRuntime.PlantWellBeing(
            wateredAt,
            nowUtc,
            energyCap,
            wellBeingSeconds
        );

        bool wasDead = plant.Energy <= 0;
        bool isDead = wellBeing <= 0;

        // Growth comes off the same stamp as the well-being, not off tick deltas: a plant grows for
        // as long as it stays watered, and the window closes when the water runs out. Anything
        // tick-based would stop a plant growing whenever its room happened to be unloaded, which is
        // most of the time for most rooms.
        int experience =
            plant.Experience + PlantGrownSecondsInWindow(wateredAt, nowUtc, wellBeingSeconds);
        int level = RoomPetRuntime.PlantLevelFor(experience, growthSeconds, maxLevel);

        // Reaching full size is what arms the plant's seed charge -- the client offers the rebreed
        // potion on a grown plant precisely because that charge can be spent and restored.
        bool canBreed = plant.CanBreed || (level >= maxLevel && plant.Level < maxLevel);

        if (wellBeing == plant.Energy && level == plant.Level && canBreed == plant.CanBreed)
        {
            return null;
        }

        // Experience stays the closed-window bank; the open window is added again on every read, so
        // persisting the sum here would double-count it at the next watering.
        PetSnapshot updated = plant with
        {
            Energy = wellBeing,
            Level = level,
            CanBreed = canBreed,
        };

        _roomGrain._state.PetsById[plant.PetId] = updated;
        motion.IsStatsDirty = true;

        if (level == plant.Level && isDead == wasDead)
        {
            // Only the bar moved. The client re-reads it when the player opens the plant, so there is
            // nothing to push -- and pushing every plant every tick is what makes a garden expensive.
            return null;
        }

        if (isDead && !wasDead)
        {
            _roomGrain._logger.LogInformation(
                "Monsterplant {PetId} withered in room {RoomId}",
                plant.PetId,
                _roomGrain.RoomId
            );
        }

        return await ToAvatarSnapshotAsync(updated, ct);
    }

    /// <summary>
    /// The client's "treat" button (header 576). Anyone in the room may treat a plant — the action
    /// only ever helps it, and the client offers it from the other-player pet menu too.
    /// </summary>
    public async Task<PetSnapshot?> TreatPlantAsync(
        ActionContext ctx,
        int petId,
        CancellationToken ct
    )
    {
        await EnsurePetsLoadedAsync(ct);

        if (
            !_roomGrain._state.PetsById.TryGetValue(petId, out PetSnapshot? plant)
            || plant.Type != MonsterplantPetType
        )
        {
            return null;
        }

        if (plant.Energy <= 0)
        {
            // A withered plant is past watering; it takes a revival potion. Refusing here is what
            // keeps "treat" from quietly resurrecting one for free.
            return null;
        }

        return await WaterPlantAsync(plant, levelsToAdd: 0, ct);
    }

    /// <summary>
    /// Harvest (header 1210): a full-grown plant hands its owner a fresh seed and spends its charge.
    /// The charge is restored by a rebreed potion, which is what the client's own copy promises —
    /// "your plant can produce new seeds. It works instantly!".
    /// </summary>
    public async Task<bool> HarvestPlantAsync(ActionContext ctx, int petId, CancellationToken ct)
    {
        await EnsurePetsLoadedAsync(ct);

        if (
            !_roomGrain._state.PetsById.TryGetValue(petId, out PetSnapshot? plant)
            || plant.Type != MonsterplantPetType
            || plant.OwnerId != ctx.PlayerId
            || plant.Level < PlantMaxLevel
            || !plant.CanBreed
            || plant.Energy <= 0
        )
        {
            return false;
        }

        int? seedDefinitionId = await ResolveSeedDefinitionIdAsync(ct);

        if (seedDefinitionId is null)
        {
            // Nothing to hand over: no furniture in this hotel is a monsterplant seed. Better to
            // refuse than to spend the charge on nothing.
            _roomGrain._logger.LogWarning(
                "Monsterplant {PetId} could not be harvested in room {RoomId}: no furniture definition is a monsterplant seed (category {Category})",
                petId,
                _roomGrain.RoomId,
                (int)FurnitureCategory.MonsterplantSeed
            );

            return false;
        }

        // Spend the charge first: the reverse order would let a repeated click mint seeds.
        PetSnapshot spent = plant with
        {
            CanBreed = false,
        };
        _roomGrain._state.PetsById[petId] = spent;
        GetMotionState(spent, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).IsStatsDirty = true;
        await FlushDirtyPetsAsync(ct);

        await _roomGrain
            ._grainFactory.GetInventoryGrain(plant.OwnerId.Value)
            .GrantFurnitureDefinitionAsync(seedDefinitionId.Value, null, ct);

        return true;
    }

    /// <summary>
    /// Compost (header 1989): the room owner turns a withered plant into nothing. The client asks
    /// for confirmation and warns that it cannot be undone, so the row goes for good.
    /// </summary>
    public async Task<bool> CompostPlantAsync(ActionContext ctx, int petId, CancellationToken ct)
    {
        await EnsurePetsLoadedAsync(ct);

        if (
            !_roomGrain._state.PetsById.TryGetValue(petId, out PetSnapshot? plant)
            || plant.Type != MonsterplantPetType
            || plant.Energy > 0
        )
        {
            return false;
        }

        // The client only offers compost to the room owner, and it is destructive: keep both ends of
        // that rule rather than trusting the button.
        if (plant.OwnerId != ctx.PlayerId && _roomGrain._state.RoomSnapshot.OwnerId != ctx.PlayerId)
        {
            return false;
        }

        await using VortexDbContext dbCtx = await _roomGrain._dbCtxFactory.CreateDbContextAsync(ct);

        PetEntity? entity = await dbCtx.Pets.FirstOrDefaultAsync(
            p => p.Id == petId && p.DeletedAt == null,
            ct
        );

        if (entity is null)
        {
            return false;
        }

        entity.RoomEntityId = null;
        entity.DeletedAt = DateTime.UtcNow;
        await dbCtx.SaveChangesAsync(ct);

        _roomGrain._state.PetsById.Remove(petId);
        _motionByPetId.Remove(petId);

        await _roomGrain.SendComposerToRoomAsync(
            new UserRemoveMessageComposer { ObjectId = RoomPetRuntime.ToRoomObjectId(petId) }
        );

        return true;
    }

    /// <summary>
    /// The whole of header 2099: a furniture used on a pet. Food and the three monsterplant potions
    /// arrive on the same packet, and the client tells them apart by the product's furniture
    /// category — so the server reads the same field rather than guessing from the pet.
    /// </summary>
    public async Task<bool> UsePetProductAsync(
        ActionContext ctx,
        int petId,
        RoomObjectId productItemId,
        CancellationToken ct
    )
    {
        FurnitureCategory category = _roomGrain._state.ItemsById.TryGetValue(
            productItemId,
            out IRoomItem? product
        )
            ? product.Definition.FurniCategory
            : FurnitureCategory.Default;

        if (
            category
            is FurnitureCategory.MonsterplantRevival
                or FurnitureCategory.MonsterplantRebreed
                or FurnitureCategory.MonsterplantFertilize
        )
        {
            return await UsePlantProductAsync(ctx, petId, productItemId, category, ct);
        }

        // Anything else keeps the old meaning: a bowl of food, which the feed path validates against
        // pet_food for this pet's type.
        await FeedPetAsync(ctx, petId, productItemId, ct);

        return true;
    }

    /// <summary>
    /// A monsterplant product used on a plant (header 2099, the same packet that feeds a pet). Which
    /// of the three it is comes from the product's own furniture category, exactly as the client
    /// decides which plants to offer it for: 20 revives a dead one, 21 restores a grown one's seed
    /// charge, 22 fertilizes one that is still growing.
    /// </summary>
    public async Task<bool> UsePlantProductAsync(
        ActionContext ctx,
        int petId,
        RoomObjectId productItemId,
        FurnitureCategory category,
        CancellationToken ct
    )
    {
        await EnsurePetsLoadedAsync(ct);

        if (
            !_roomGrain._state.PetsById.TryGetValue(petId, out PetSnapshot? plant)
            || plant.Type != MonsterplantPetType
        )
        {
            return false;
        }

        bool isDead = plant.Energy <= 0;

        bool applicable = category switch
        {
            FurnitureCategory.MonsterplantRevival => isDead,
            FurnitureCategory.MonsterplantRebreed => !isDead
                && plant.Level >= PlantMaxLevel
                && !plant.CanBreed,
            FurnitureCategory.MonsterplantFertilize => !isDead && plant.Level < PlantMaxLevel,
            _ => false,
        };

        if (!applicable)
        {
            return false;
        }

        if (!_roomGrain._state.ItemsById.TryGetValue(productItemId, out IRoomItem? product))
        {
            return false;
        }

        // Consume first for the same reason the mystery box does: a failed grant costs the player
        // one potion, while the other order lets a repeated click apply one potion twice.
        await _roomGrain.ObjectModule.RemoveObjectAsync(ctx, product, ct, product.OwnerId);

        switch (category)
        {
            case FurnitureCategory.MonsterplantRevival:
                await WaterPlantAsync(plant, levelsToAdd: 0, ct);
                break;

            case FurnitureCategory.MonsterplantRebreed:
                await SetSeedChargeAsync(plant, ct);
                break;

            case FurnitureCategory.MonsterplantFertilize:
                await WaterPlantAsync(plant, Math.Max(1, Tuning.PlantFertilizerLevels), ct);
                break;
        }

        return true;
    }

    /// <summary>
    /// Refills the well-being clock and optionally skips growth stages. The stamp is what the client
    /// counts down from, so resetting it <em>is</em> the watering.
    /// </summary>
    private async Task<PetSnapshot> WaterPlantAsync(
        PetSnapshot plant,
        int levelsToAdd,
        CancellationToken ct
    )
    {
        int growthSeconds = Math.Max(1, Tuning.PlantGrowthSeconds);
        int wellBeingSeconds = Math.Max(1, Tuning.PlantWellBeingSeconds);
        int energyCap = _roomGrain._petLevelProvider.GetEnergyCapForLevel(plant.Type, plant.Level);

        // Watering closes the window that was open: whatever the plant grew on the last can of water
        // is banked now, and the new window starts from zero.
        int experience =
            plant.Experience
            + PlantGrownSecondsInWindow(
                plant.LastWateredAt ?? plant.CreatedAt,
                DateTime.UtcNow,
                wellBeingSeconds
            )
            + (Math.Max(0, levelsToAdd) * growthSeconds);
        int level = RoomPetRuntime.PlantLevelFor(experience, growthSeconds, PlantMaxLevel);

        PetSnapshot updated = plant with
        {
            Energy = energyCap,
            LastWateredAt = DateTime.UtcNow,
            Experience = experience,
            Level = level,
            CanBreed = plant.CanBreed || level >= PlantMaxLevel,
        };

        _roomGrain._state.PetsById[plant.PetId] = updated;

        GetMotionState(updated, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).IsStatsDirty = true;

        // Straight to the database: watering is a player action with a visible countdown, and the
        // next stat flush could be a minute away.
        await FlushDirtyPetsAsync(ct);
        await BroadcastPlantAsync(updated, ct);

        return updated;
    }

    private async Task SetSeedChargeAsync(PetSnapshot plant, CancellationToken ct)
    {
        PetSnapshot updated = plant with { CanBreed = true };
        _roomGrain._state.PetsById[plant.PetId] = updated;
        GetMotionState(updated, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).IsStatsDirty = true;

        await FlushDirtyPetsAsync(ct);
    }

    private async Task BroadcastPlantAsync(PetSnapshot plant, CancellationToken ct)
    {
        RoomPetAvatarSnapshot snapshot = await ToAvatarSnapshotAsync(plant, ct);

        await _roomGrain.SendComposerToRoomAsync(
            new UserUpdateMessageComposer { Avatars = [snapshot] }
        );
    }

    /// <summary>
    /// How much of the current watering window the plant actually grew through: the window closes
    /// when the water runs out, so a plant left dead for a week banks a day, not a week.
    /// </summary>
    private static int PlantGrownSecondsInWindow(
        DateTime wateredAt,
        DateTime now,
        int wellBeingSeconds
    ) => (int)Math.Clamp((now - wateredAt).TotalSeconds, 0, wellBeingSeconds);

    /// <summary>
    /// The furniture a harvested seed hands over: whichever definition this hotel files under the
    /// client's own monsterplant-seed category. Lowest id wins so the answer is stable.
    /// </summary>
    private async Task<int?> ResolveSeedDefinitionIdAsync(CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _roomGrain._dbCtxFactory.CreateDbContextAsync(ct);

        return await dbCtx
            .FurnitureDefinitions.AsNoTracking()
            .Where(d => d.FurniCategory == FurnitureCategory.MonsterplantSeed)
            .OrderBy(d => d.Id)
            .Select(d => (int?)d.Id)
            .FirstOrDefaultAsync(ct);
    }
}
