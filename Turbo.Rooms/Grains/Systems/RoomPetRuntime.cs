using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Context;
using Turbo.Database.Entities.Furniture;
using Turbo.Database.Entities.Pets;
using Turbo.Primitives.Pets.Snapshots;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Snapshots.Avatars;

namespace Turbo.Rooms.Grains.Systems;

internal static class RoomPetRuntime
{
    private const int PetRoomObjectIdOffset = 1_000_000;
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
            HasSaddle = false,
            IsRiding = false,
            CanBreed = pet.Type != MonsterplantPetType && pet.CanBreed,
            CanHarvest = pet.Type == MonsterplantPetType && pet.Level >= MonsterplantMaxLevel,
            CanRevive = pet.Type == MonsterplantPetType && pet.Energy == 0,
            HasBreedingPermission = pet.Type != MonsterplantPetType && pet.CanBreed,
            PetLevel = pet.Level,
            PetPosture = posture,
        };

    private static string ToFigureString(PetSnapshot pet) =>
        pet.Type == MonsterplantPetType
            ? $"{pet.Type} {pet.Level} {pet.Color} 0"
            : $"{pet.Type} {pet.Race} {pet.Color} 0";

    public static async Task<PetFeedResult> FeedAsync(
        TurboDbContext dbCtx,
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
