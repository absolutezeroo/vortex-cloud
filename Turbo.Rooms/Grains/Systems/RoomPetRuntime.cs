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

namespace Turbo.Rooms.Grains.Systems;

internal static class RoomPetRuntime
{
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
            X = entity.X,
            Y = entity.Y,
            Z = entity.Z,
            Direction = (Rotation)entity.Direction,
        };

    public static async Task<PetFeedResult> FeedAsync(
        TurboDbContext dbCtx,
        int roomId,
        int actorPlayerId,
        int petId,
        RoomObjectId foodItemId,
        bool allowPetsEat,
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

        if (petFood is null || petFood.Nutrition <= 0)
        {
            return PetFeedResult.Failed(foodItemId);
        }

        int nutritionBefore = pet.Nutrition;

        pet.X = food.X;
        pet.Y = food.Y;
        pet.Z = food.Z;
        pet.Direction = (int)food.Rotation;
        pet.Nutrition += petFood.Nutrition;

        food.RoomEntityId = null;
        food.DeletedAt = DateTime.UtcNow;

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);

        return new PetFeedResult
        {
            Success = true,
            Pet = ToSnapshot(pet),
            FoodItemId = foodItemId,
            NutritionAdded = petFood.Nutrition,
            NutritionBefore = nutritionBefore,
            NutritionAfter = pet.Nutrition,
        };
    }
}
