using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Database.Entities.Pets;
using Vortex.Database.Entities.Players;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Navigator.Enums;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Rooms.Grains.Systems;
using Xunit;

namespace Vortex.Rooms.Tests.Pets;

/// <summary>
/// Feeding is the only pet interaction that spends a real item out of the room, so a refusal has to
/// land before the bowl is touched and an accepted feed has to spend exactly one use. Every guard
/// below is one a hostile or merely stale client can reach: the pet id, the bowl id and the room's
/// own eating rule all arrive from outside.
/// </summary>
public sealed class PetFeedingTests
{
    private const int RoomId = 10;
    private const int OwnerId = 1;
    private const int StrangerId = 2;
    private const int PetId = 500;
    private const int PetType = 0;
    private const int FoodDefinitionId = 300;
    private const int FoodItemId = 400;

    [Fact]
    public async Task FeedAsync_MatchingFood_IncreasesNutritionAndConsumesFood()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();

        await SeedFeedScenarioAsync(options).ConfigureAwait(true);

        using VortexDbContext db = new VortexDbContext(options);

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

    [Fact]
    public async Task FeedAsync_RoomForbidsEating_RefusesBeforeSpendingTheBowl()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        await SeedFeedScenarioAsync(options).ConfigureAwait(true);

        using VortexDbContext db = new(options);

        PetFeedResult result = await FeedAsync(db, allowPetsEat: false).ConfigureAwait(true);

        result.Success.Should().BeFalse();
        result.Pet.Should().BeNull();
        await AssertBowlUntouchedAsync(db).ConfigureAwait(true);
    }

    [Fact]
    public async Task FeedAsync_SomeoneElsesPet_IsRefused()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        await SeedFeedScenarioAsync(options).ConfigureAwait(true);

        using VortexDbContext db = new(options);

        PetFeedResult result = await FeedAsync(db, actorPlayerId: StrangerId).ConfigureAwait(true);

        result.Success.Should().BeFalse();
        await AssertBowlUntouchedAsync(db).ConfigureAwait(true);
    }

    [Fact]
    public async Task FeedAsync_FoodRegisteredForAnotherSpecies_IsRefused()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        // The bowl is real and in the room; it is simply not food for this kind of pet.
        await SeedFeedScenarioAsync(options, foodPetType: PetType + 1).ConfigureAwait(true);

        using VortexDbContext db = new(options);

        PetFeedResult result = await FeedAsync(db).ConfigureAwait(true);

        result.Success.Should().BeFalse();
        await AssertBowlUntouchedAsync(db).ConfigureAwait(true);
    }

    [Fact]
    public async Task FeedAsync_FurnitureThatIsNotFood_IsRefused()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        await SeedFeedScenarioAsync(options, registerPetFood: false).ConfigureAwait(true);

        using VortexDbContext db = new(options);

        PetFeedResult result = await FeedAsync(db).ConfigureAwait(true);

        result.Success.Should().BeFalse();
        await AssertBowlUntouchedAsync(db).ConfigureAwait(true);
    }

    [Fact]
    public async Task FeedAsync_BowlInAnotherRoom_IsRefused()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        await SeedFeedScenarioAsync(options).ConfigureAwait(true);

        using VortexDbContext db = new(options);

        // Same bowl id, a room the actor is not in: the query is scoped by room precisely so a
        // client cannot reach across rooms by guessing an item id.
        PetFeedResult result = await RoomPetRuntime
            .FeedAsync(
                db,
                roomId: RoomId + 1,
                actorPlayerId: OwnerId,
                petId: PetId,
                foodItemId: new RoomObjectId(FoodItemId),
                allowPetsEat: true,
                nutritionCap: 100,
                energyCap: 100,
                CancellationToken.None
            )
            .ConfigureAwait(true);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task FeedAsync_NeitherNeedGoesPastItsCap()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        await SeedFeedScenarioAsync(options, petNutrition: 98, petEnergy: 95, foodEnergy: 10)
            .ConfigureAwait(true);

        using VortexDbContext db = new(options);

        PetFeedResult result = await FeedAsync(db).ConfigureAwait(true);

        result.Success.Should().BeTrue();
        result.NutritionAfter.Should().Be(100, "nutrition is clamped to the cap, not overshot");
        result.Pet!.Energy.Should().Be(100);
    }

    [Fact]
    public async Task FeedAsync_FoodCarryingEnergy_RestoresItToo()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        await SeedFeedScenarioAsync(options, petEnergy: 20, foodEnergy: 10).ConfigureAwait(true);

        using VortexDbContext db = new(options);

        PetFeedResult result = await FeedAsync(db).ConfigureAwait(true);

        result.Pet!.Energy.Should().Be(30);
    }

    [Fact]
    public async Task FeedAsync_TheLastUse_TakesTheBowlOutOfTheRoom()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        await SeedFeedScenarioAsync(options, maxUses: 1).ConfigureAwait(true);

        using VortexDbContext db = new(options);

        PetFeedResult result = await FeedAsync(db).ConfigureAwait(true);

        result.Success.Should().BeTrue();
        result.UsesRemaining.Should().Be(0);

        FurnitureEntity bowl = await db
            .Furnitures.IgnoreQueryFilters()
            .SingleAsync(f => f.Id == FoodItemId)
            .ConfigureAwait(true);
        bowl.RoomEntityId.Should()
            .BeNull("an emptied bowl leaves the room rather than sitting there");
        bowl.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task FeedAsync_APartlyUsedBowl_CountsDownFromWhatIsLeft()
    {
        DbContextOptions<VortexDbContext> options = NewOptions();
        // ExtraData is the authority once the bowl has been used; MaxUses only seeds a fresh one.
        await SeedFeedScenarioAsync(options, maxUses: 5, extraData: "2").ConfigureAwait(true);

        using VortexDbContext db = new(options);

        PetFeedResult result = await FeedAsync(db).ConfigureAwait(true);

        result.UsesRemaining.Should().Be(1);
    }

    private static Task<PetFeedResult> FeedAsync(
        VortexDbContext db,
        int actorPlayerId = OwnerId,
        bool allowPetsEat = true
    ) =>
        RoomPetRuntime.FeedAsync(
            db,
            roomId: RoomId,
            actorPlayerId: actorPlayerId,
            petId: PetId,
            foodItemId: new RoomObjectId(FoodItemId),
            allowPetsEat: allowPetsEat,
            nutritionCap: 100,
            energyCap: 100,
            CancellationToken.None
        );

    private static async Task AssertBowlUntouchedAsync(VortexDbContext db)
    {
        FurnitureEntity bowl = await db
            .Furnitures.IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(f => f.Id == FoodItemId)
            .ConfigureAwait(true);

        bowl.DeletedAt.Should().BeNull();
        bowl.RoomEntityId.Should().Be(RoomId);
        bowl.ExtraData.Should().BeNull("a refused feed must not spend a use");
    }

    private static DbContextOptions<VortexDbContext> NewOptions() =>
        new DbContextOptionsBuilder<VortexDbContext>()
            .UseInMemoryDatabase($"pet-feed-{Guid.NewGuid():N}")
            .Options;

    private static async Task SeedFeedScenarioAsync(
        DbContextOptions<VortexDbContext> options,
        int petNutrition = 3,
        int petEnergy = 20,
        int foodNutrition = 7,
        int foodEnergy = 0,
        int maxUses = 5,
        int? foodPetType = null,
        bool registerPetFood = true,
        string? extraData = null
    )
    {
        using VortexDbContext db = new VortexDbContext(options);

        PlayerEntity player = new()
        {
            Id = OwnerId,
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
            Id = RoomId,
            Name = "Pet room",
            PlayerEntityId = player.Id,
            DoorMode = RoomDoorModeType.Open,
            RoomModelEntityId = model.Id,
            UsersNow = 0,
            PlayersMax = 25,
            PaintWall = string.Empty,
            PaintFloor = string.Empty,
            PaintLandscape = string.Empty,
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
            Score = 0,
            IsStaffPick = false,
            PlayerEntity = player,
            RoomModelEntity = model,
        };

        FurnitureDefinitionEntity foodDefinition = new()
        {
            Id = FoodDefinitionId,
            SpriteId = 30,
            Name = "pet_food_bowl",
            ProductType = ProductType.Floor,
            FurniCategory = FurnitureCategory.Default,
            Logic = "pet_food",
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
            Id = FoodItemId,
            PlayerEntityId = player.Id,
            FurnitureDefinitionEntityId = foodDefinition.Id,
            RoomEntityId = room.Id,
            X = 5,
            Y = 6,
            Z = 1.25,
            Rotation = Rotation.East,
            ExtraData = extraData,
        };

        PetEntity pet = new()
        {
            Id = PetId,
            OwnerPlayerEntityId = player.Id,
            RoomEntityId = room.Id,
            Name = "Biscuit",
            Type = PetType,
            Race = 1,
            Color = "FFFFFF",
            Gender = AvatarGenderType.Male,
            Level = 1,
            Experience = 0,
            Energy = petEnergy,
            Nutrition = petNutrition,
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
        db.Pets.Add(pet);

        if (registerPetFood)
        {
            db.PetFood.Add(
                new PetFoodEntity
                {
                    FurnitureDefinitionEntityId = foodDefinition.Id,
                    PetType = foodPetType ?? PetType,
                    Nutrition = foodNutrition,
                    Energy = foodEnergy,
                    MaxUses = maxUses,
                    FurnitureDefinitionEntity = foodDefinition,
                }
            );
        }

        await db.SaveChangesAsync().ConfigureAwait(false);
    }
}
