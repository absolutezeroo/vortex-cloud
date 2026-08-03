using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Database.Entities.Pets;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Snapshots.Avatars;

namespace Vortex.Rooms.Grains.Systems;

internal static class RoomPetRuntime
{
    private const int PetRoomObjectIdOffset = 1_000_000;

    /// <summary>The coordinates the client sends when a pet is dropped from the inventory rather
    /// than aimed at a tile.</summary>
    public const int InventoryDropX = 0;
    public const int InventoryDropY = 0;

    private const int MonsterplantPetType = 16;
    private const int MonsterplantMaxLevel = 7;

    public static PetSnapshot ToSnapshot(PetEntity entity) =>
        new()
        {
            PetId = entity.Id,
            OwnerId = new PlayerId(entity.OwnerPlayerEntityId),
            RoomId = entity.RoomEntityId,
            Name = entity.Name,
            Type = entity.Type,
            Race = entity.Race,
            Color = entity.Color,
            Gender = entity.Gender,
            Level = entity.Level,
            Experience = entity.Experience,
            Energy = entity.Energy,
            Nutrition = entity.Nutrition,
            Respect = entity.Respect,
            RespectTodayCount = entity.RespectTodayCount,
            RespectLastResetDate = entity.RespectLastResetDate,
            ParentOneId = entity.ParentOneId,
            ParentTwoId = entity.ParentTwoId,
            CanBreed = entity.CanBreed,
            RarityLevel = entity.RarityLevel,
            LastWateredAt = entity.LastWateredAt,
            X = entity.X,
            Y = entity.Y,
            Z = entity.Z,
            Direction = (Rotation)entity.Direction,
        };

    public static RoomObjectId ToRoomObjectId(int petId) => petId + PetRoomObjectIdOffset;

    public static RoomPetAvatarSnapshot ToAvatarSnapshot(
        PetSnapshot pet,
        string ownerName,
        string status = "",
        string posture = ""
    ) =>
        new()
        {
            AvatarType = RoomObjectType.Pet,
            WebId = pet.PetId,
            Name = pet.Name,
            Motto = string.Empty,
            Figure = ToFigureString(pet),
            ObjectId = ToRoomObjectId(pet.PetId),
            X = pet.X,
            Y = pet.Y,
            Z = pet.Z,
            BodyRotation = pet.Direction,
            HeadRotation = pet.Direction,
            Status = status,
            SubType = pet.Type,
            OwnerId = pet.OwnerId.Value,
            OwnerName = ownerName,
            RarityLevel = pet.RarityLevel,
            HasSaddle = pet.HasSaddle,
            IsRiding = pet.RiderId is not null,
            CanBreed = CanBreed(pet),
            CanHarvest = CanHarvest(pet),
            CanRevive = CanRevive(pet),
            HasBreedingPermission = HasBreedingPermission(pet),
            PetLevel = pet.Level,
            PetPosture = posture,
        };

    /// <summary>
    /// Where a pet dropped from the inventory lands.
    ///
    /// The client hard-codes (0, 0) for that drop -- there is no tile to name yet -- so those
    /// coordinates mean "anywhere that works", not the corner of the room. Read literally they
    /// rejected the placement whenever tile 0,0 was blocked, which is most rooms, and left
    /// non-owners unable to place a pet at all where the room allowed them.
    ///
    /// Any other coordinates are the player pointing at a tile and are honoured as given.
    ///
    /// The search runs outwards from the door in rings, so a pet appears near where players arrive
    /// rather than in whichever corner is scanned first.
    /// </summary>
    public static bool TryResolveDropTile(
        int x,
        int y,
        int doorX,
        int doorY,
        int width,
        int height,
        Func<int, int, bool> isFree,
        out int resolvedX,
        out int resolvedY
    )
    {
        resolvedX = x;
        resolvedY = y;

        if (x != InventoryDropX || y != InventoryDropY)
        {
            return isFree(x, y);
        }

        int reach = Math.Max(width, height);

        for (int radius = 0; radius <= reach; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    // Only the ring at this radius; the inside was covered by earlier passes.
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                    {
                        continue;
                    }

                    if (!isFree(doorX + dx, doorY + dy))
                    {
                        continue;
                    }

                    resolvedX = doorX + dx;
                    resolvedY = doorY + dy;

                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Which actions a pet offers. Shared with the status push so the room list and the status
    /// message can never disagree about whether a pet can be bred, harvested or revived.
    /// </summary>
    public static bool CanBreed(PetSnapshot pet) => pet.Type != MonsterplantPetType && pet.CanBreed;

    public static bool CanHarvest(PetSnapshot pet) =>
        pet.Type == MonsterplantPetType && pet.Level >= MonsterplantMaxLevel;

    public static bool CanRevive(PetSnapshot pet) =>
        pet.Type == MonsterplantPetType && pet.Energy == 0;

    public static bool HasBreedingPermission(PetSnapshot pet) =>
        pet.Type != MonsterplantPetType && pet.CanBreed;

    /// <summary>
    /// Takes the whole points a need has accrued since its own clock, and moves that clock forward
    /// by only the time those points cost -- never to <paramref name="now" />, so the leftover
    /// fraction carries into the next tick.
    /// </summary>
    /// <remarks>
    /// Nutrition and energy each need their own clock. Sharing one and resetting it to now the
    /// moment either need ticked let the faster need starve the slower one: nutrition at 1.0/min
    /// reset the accumulator every minute, so energy at 0.5/min never reached a whole point. Pets
    /// never tired, never got thirsty and never fell asleep.
    /// </remarks>
    public static int TakeWholeNeedPoints(
        long clockMs,
        long now,
        double perMinute,
        out long nextClockMs
    )
    {
        nextClockMs = clockMs;

        if (perMinute <= 0)
        {
            nextClockMs = now;

            return 0;
        }

        long elapsedMs = now - clockMs;

        if (elapsedMs <= 0)
        {
            return 0;
        }

        int points = (int)(elapsedMs / 60_000.0 * perMinute);

        if (points <= 0)
        {
            return 0;
        }

        nextClockMs = clockMs + (long)(points / perMinute * 60_000.0);

        return points;
    }

    private static string ToFigureString(PetSnapshot pet) =>
        pet.Type == MonsterplantPetType
            ? $"{pet.Type} {pet.Level} {pet.Color} 0"
            : $"{pet.Type} {pet.Race} {pet.Color} 0";

    public static async Task<PetFeedResult> FeedAsync(
        VortexDbContext dbCtx,
        int roomId,
        int actorPlayerId,
        int petId,
        RoomObjectId foodItemId,
        bool allowPetsEat,
        int nutritionCap,
        int energyCap,
        CancellationToken ct
    )
    {
        if (!allowPetsEat)
        {
            return PetFeedResult.Failed(foodItemId);
        }

        PetEntity? pet = await dbCtx
            .Pets.SingleOrDefaultAsync(
                p => p.Id == petId && p.RoomEntityId == roomId && p.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

        if (pet is null || pet.OwnerPlayerEntityId != actorPlayerId)
        {
            return PetFeedResult.Failed(foodItemId);
        }

        FurnitureEntity? food = await dbCtx
            .Furnitures.SingleOrDefaultAsync(
                f => f.Id == foodItemId.Value && f.RoomEntityId == roomId && f.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

        if (food is null)
        {
            return PetFeedResult.Failed(foodItemId);
        }

        PetFoodEntity? petFood = await dbCtx
            .PetFood.AsNoTracking()
            .SingleOrDefaultAsync(
                f =>
                    f.FurnitureDefinitionEntityId == food.FurnitureDefinitionEntityId
                    && f.PetType == pet.Type
                    && f.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

        if (petFood is null || (petFood.Nutrition <= 0 && petFood.Energy <= 0))
        {
            return PetFeedResult.Failed(foodItemId);
        }

        int nutritionBefore = pet.Nutrition;

        pet.X = food.X;
        pet.Y = food.Y;
        pet.Z = food.Z;
        pet.Direction = (int)food.Rotation;
        pet.Nutrition = Math.Min(pet.Nutrition + petFood.Nutrition, nutritionCap);
        pet.Energy = Math.Min(pet.Energy + petFood.Energy, energyCap);

        int currentUses = int.TryParse(food.ExtraData, out int parsed) ? parsed : petFood.MaxUses;
        int usesRemaining = Math.Max(0, currentUses - 1);

        if (usesRemaining == 0)
        {
            food.RoomEntityId = null;
            food.DeletedAt = DateTime.UtcNow;
        }
        else
        {
            food.ExtraData = usesRemaining.ToString();
        }

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        return new PetFeedResult
        {
            Success = true,
            Pet = ToSnapshot(pet),
            FoodItemId = foodItemId,
            NutritionAdded = petFood.Nutrition,
            NutritionBefore = nutritionBefore,
            NutritionAfter = pet.Nutrition,
            UsesRemaining = usesRemaining,
            FoodState = usesRemaining,
        };
    }
}
