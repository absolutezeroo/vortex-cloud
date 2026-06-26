using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Context;
using Turbo.Database.Entities.Furniture;
using Turbo.Database.Entities.Pets;
using Turbo.Database.Entities.Players;
using Turbo.Database.Entities.Room;
using Turbo.Primitives.Furniture.Enums;
using Turbo.Primitives.Navigator.Enums;
using Turbo.Primitives.Pets.Snapshots;
using Turbo.Primitives.Players.Enums;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Object;
using Turbo.Rooms.Grains.Systems;
using Xunit;

namespace Turbo.Rooms.Tests.Pets;

public sealed class PetFeedingTests
{
    [Fact]
    public async Task FeedAsync_MatchingFood_IncreasesNutritionAndConsumesFood()
    {
        DbContextOptions<TurboDbContext> options = NewOptions();

        await SeedFeedScenarioAsync(options).ConfigureAwait(true);

        using TurboDbContext db = new TurboDbContext(options);

        PetFeedResult result = await RoomPetRuntime
            .FeedAsync(
                db,
                roomId: 10,
                actorPlayerId: 1,
                petId: 500,
                foodItemId: new RoomObjectId(400),
                allowPetsEat: true,
                nutritionCap: 100,
                energyCap: 100,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        result.Success.Should().BeTrue();
        result.NutritionAdded.Should().Be(7);
        result.NutritionBefore.Should().Be(3);
        result.NutritionAfter.Should().Be(10);
        result.UsesRemaining.Should().Be(4, "MaxUses=5 minus one use");
        result.FoodState.Should().Be(4, "descending: MaxUses=5, first use leaves 4 remaining");
        result.Pet.Should().NotBeNull();
        result.Pet!.X.Should().Be(5);
        result.Pet.Y.Should().Be(6);
        result.Pet.Z.Should().Be(1.25);
        result.Pet.Direction.Should().Be(Rotation.East);

        PetEntity pet = await db.Pets.SingleAsync(p => p.Id == 500).ConfigureAwait(true);
        pet.Nutrition.Should().Be(10);
        pet.X.Should().Be(5);
        pet.Y.Should().Be(6);
        pet.Z.Should().Be(1.25);
        pet.Direction.Should().Be((int)Rotation.East);

        FurnitureEntity food = await db
            .Furnitures.SingleAsync(f => f.Id == 400)
            .ConfigureAwait(true);
        food.RoomEntityId.Should().Be(10, "bowl stays in room while uses remain");
        food.DeletedAt.Should().BeNull("bowl is not deleted until all uses are exhausted");
        food.ExtraData.Should().Be("4", "descending: MaxUses=5, first use leaves 4 remaining");
    }

    private static DbContextOptions<TurboDbContext> NewOptions() =>
        new DbContextOptionsBuilder<TurboDbContext>()
            .UseInMemoryDatabase($"pet-feed-{Guid.NewGuid():N}")
            .Options;

    private static async Task SeedFeedScenarioAsync(DbContextOptions<TurboDbContext> options)
    {
        using TurboDbContext db = new TurboDbContext(options);

        PlayerEntity player = new()
        {
            Id = 1,
            Name = "Owner",
            Figure = "hr-115",
            Gender = AvatarGenderType.Male,
            PlayerStatus = PlayerStatusType.Offline,
            PlayerPerks = PlayerPerkFlags.None,
        };

        RoomModelEntity model = new()
        {
            Id = 100,
            Name = "model",
            Model = "0",
            DoorX = 0,
            DoorY = 0,
            DoorRotation = Rotation.South,
            Enabled = true,
            Custom = false,
        };

        RoomEntity room = new()
        {
            Id = 10,
            Name = "Pet room",
            PlayerEntityId = player.Id,
            DoorMode = RoomDoorModeType.Open,
            RoomModelEntityId = model.Id,
            UsersNow = 0,
            PlayersMax = 25,
            PaintWall = 0,
            PaintFloor = 0,
            PaintLandscape = 0,
            WallHeight = -1,
            HideWalls = false,
            ThicknessWall = RoomThicknessType.Normal,
            ThicknessFloor = RoomThicknessType.Normal,
            AllowBlocking = false,
            AllowPets = true,
            AllowPetsEat = true,
            TradeType = RoomTradeModeType.Disabled,
            MuteType = ModSettingType.Owner,
            KickType = ModSettingType.Owner,
            BanType = ModSettingType.Owner,
            ChatModeType = ChatModeType.FreeFlow,
            ChatBubbleType = ChatBubbleWidthType.Normal,
            ChatSpeedType = ChatScrollSpeedType.Normal,
            ChatFloodType = ChatFloodSensitivityType.Minimal,
            ChatDistance = 50,
            PlayerEntity = player,
            RoomModelEntity = model,
        };

        FurnitureDefinitionEntity foodDefinition = new()
        {
            Id = 300,
            SpriteId = 30,
            Name = "pet_food_bowl",
            ProductType = ProductType.Floor,
            FurniCategory = FurnitureCategory.Default,
            Logic = "furniture_pet_product",
            Width = 1,
            Length = 1,
            StackHeight = 0.1,
            CanStack = false,
            CanWalk = true,
            CanSit = false,
            CanLay = false,
            CanRecycle = false,
            CanTrade = false,
            CanGroup = false,
            CanSell = false,
        };

        FurnitureEntity food = new()
        {
            Id = 400,
            PlayerEntityId = player.Id,
            FurnitureDefinitionEntityId = foodDefinition.Id,
            RoomEntityId = room.Id,
            X = 5,
            Y = 6,
            Z = 1.25,
            Rotation = Rotation.East,
        };

        PetFoodEntity petFood = new()
        {
            FurnitureDefinitionEntityId = foodDefinition.Id,
            PetType = 0,
            Nutrition = 7,
            Energy = 0,
            MaxUses = 5,
            FurnitureDefinitionEntity = foodDefinition,
        };

        PetEntity pet = new()
        {
            Id = 500,
            OwnerPlayerEntityId = player.Id,
            RoomEntityId = room.Id,
            Name = "Biscuit",
            Type = 0,
            Race = 1,
            Color = "FFFFFF",
            Gender = AvatarGenderType.Male,
            Level = 1,
            Experience = 0,
            Energy = 20,
            Nutrition = 3,
            Respect = 0,
            X = 1,
            Y = 1,
            Z = 0,
            Direction = (int)Rotation.South,
            OwnerPlayerEntity = player,
            RoomEntity = room,
        };

        db.Players.Add(player);
        db.RoomModels.Add(model);
        db.Rooms.Add(room);
        db.FurnitureDefinitions.Add(foodDefinition);
        db.Furnitures.Add(food);
        db.PetFood.Add(petFood);
        db.Pets.Add(pet);

        await db.SaveChangesAsync().ConfigureAwait(false);
    }
}
