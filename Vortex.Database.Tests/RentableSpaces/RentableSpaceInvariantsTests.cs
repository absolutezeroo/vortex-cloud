using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Database.Entities.Furniture;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Rooms.Enums;
using Xunit;

namespace Vortex.Database.Tests.RentableSpaces;

/// <summary>
///     Covers the DATA-MODEL §3 invariants for ROADMAP Epic 4 / rentable space:
///     · one state row per furniture instance (§3.2 unique index)
///     · one rented space per player at a time (§3.4 — enforced via renter_player_id index + app logic)
///     · furniture tag self-FK is non-cascade (§3.3 — bulk-null on expiry must not cascade-delete)
///     · state row cleared in-place on expiry: renter / rented_until become null, row is not deleted
/// </summary>
public sealed class RentableSpaceInvariantsTests
{
    private static TurboDbContext NewContext()
    {
        return new TurboDbContext(
            new DbContextOptionsBuilder<TurboDbContext>()
                .UseInMemoryDatabase($"rentable-{Guid.NewGuid():N}")
                .Options
        );
    }

    private static (
        PlayerEntity player,
        FurnitureDefinitionEntity definition,
        FurnitureEntity furniture,
        RentableSpaceTermsEntity terms
    ) Seed(TurboDbContext ctx, int playerId = 1, int furniId = 10)
    {
        CurrencyTypeEntity currency = new()
        {
            Name = "Credits",
            CurrencyType = CurrencyType.Credits,
            Enabled = true,
        };
        ctx.CurrencyTypes.Add(currency);

        PlayerEntity player = new()
        {
            Id = playerId,
            Name = $"Player{playerId}",
            Figure = "hr-115",
            Gender = AvatarGenderType.Male,
            PlayerStatus = PlayerStatusType.Offline,
            PlayerPerks = PlayerPerkFlags.None,
        };
        ctx.Players.Add(player);

        FurnitureDefinitionEntity definition = new()
        {
            SpriteId = 1,
            Name = "rent_space_1x1",
            ProductType = ProductType.Floor,
            FurniCategory = FurnitureCategory.Default,
            Logic = "rentable_space",
            Width = 1,
            Length = 1,
            StackHeight = 1.0,
            CanStack = false,
            CanWalk = false,
            CanSit = false,
            CanLay = false,
            CanRecycle = false,
            CanTrade = false,
            CanGroup = false,
            CanSell = false,
        };
        ctx.FurnitureDefinitions.Add(definition);

        FurnitureEntity furniture = new()
        {
            Id = furniId,
            PlayerEntityId = playerId,
            FurnitureDefinitionEntityId = definition.Id,
        };
        ctx.Furnitures.Add(furniture);

        RentableSpaceTermsEntity terms = new()
        {
            FurnitureEntityId = furniture.Id,
            Price = 10,
            CurrencyTypeEntityId = currency.Id,
            RentDurationSeconds = 3600,
            RequiresHc = false,
            FurnitureEntity = furniture,
            CurrencyTypeEntity = currency,
        };
        ctx.RentableSpaceTerms.Add(terms);

        ctx.SaveChanges();
        return (player, definition, furniture, terms);
    }

    [Fact]
    public void StateRow_IsUniquePerFurnitureInstance()
    {
        using TurboDbContext ctx = NewContext();
        (_, _, FurnitureEntity furniture, _) = Seed(ctx);

        RoomRentableSpaceEntity space1 = new()
        {
            FurnitureEntityId = furniture.Id,
            FurnitureEntity = furniture,
        };
        ctx.RoomRentableSpaces.Add(space1);
        ctx.SaveChanges();

        // A second row for the same furniture must violate the unique index.
        // InMemory DB does not enforce unique indexes — we verify at the model level instead.
        IIndex? uniqueIndex = ctx
            .Model.FindEntityType(typeof(RoomRentableSpaceEntity))!
            .GetIndexes()
            .SingleOrDefault(i =>
                i.IsUnique
                && i.Properties.Any(p =>
                    p.Name == nameof(RoomRentableSpaceEntity.FurnitureEntityId)
                )
            );

        uniqueIndex.Should().NotBeNull("room_rentable_spaces.furniture_id must be unique (§3.2)");
    }

    [Fact]
    public void StateRow_ClearedInPlace_OnExpiry()
    {
        using TurboDbContext ctx = NewContext();
        (PlayerEntity player, _, FurnitureEntity furniture, _) = Seed(ctx);

        RoomRentableSpaceEntity space = new()
        {
            FurnitureEntityId = furniture.Id,
            FurnitureEntity = furniture,
            RenterPlayerEntityId = player.Id,
            RentedUntil = DateTime.UtcNow.AddHours(1),
        };
        ctx.RoomRentableSpaces.Add(space);
        ctx.SaveChanges();

        // Simulate expiry: clear renter + rented_until in-place (no new row, no hard delete).
        RoomRentableSpaceEntity row = ctx.RoomRentableSpaces.Single(s =>
            s.FurnitureEntityId == furniture.Id
        );
        row.RenterPlayerEntityId = null;
        row.RentedUntil = null;
        ctx.SaveChanges();

        RoomRentableSpaceEntity after = ctx.RoomRentableSpaces.Single(s =>
            s.FurnitureEntityId == furniture.Id
        );
        after.RenterPlayerEntityId.Should().BeNull("expiry clears the renter, not deletes the row");
        after.RentedUntil.Should().BeNull("expiry clears rented_until, not deletes the row");
        after.DeletedAt.Should().BeNull("soft-delete is not triggered on expiry — row stays");
    }

    [Fact]
    public void TaggedFurniture_SelfFk_IsNonCascade()
    {
        using TurboDbContext ctx = NewContext();

        IForeignKey? fkConfig = ctx
            .Model.FindEntityType(typeof(FurnitureEntity))!
            .GetForeignKeys()
            .SingleOrDefault(fk =>
                fk.Properties.Any(p =>
                    p.Name == nameof(FurnitureEntity.RentableSpaceFurnitureEntityId)
                )
            );

        fkConfig.Should().NotBeNull("FurnitureEntity must have a self-FK for the space tag (§3.3)");
        fkConfig!
            .DeleteBehavior.Should()
            .NotBe(
                DeleteBehavior.Cascade,
                "deleting a space furni must not cascade-delete tagged items (§3.3)"
            );
    }

    [Fact]
    public void FurnitureTag_SetAndClearedInBulk()
    {
        using TurboDbContext ctx = NewContext();
        (PlayerEntity player, FurnitureDefinitionEntity definition, FurnitureEntity spaceFurni, _) =
            Seed(ctx);

        // Renter places two items inside the space — tag them.
        FurnitureEntity item1 = new()
        {
            PlayerEntityId = player.Id,
            FurnitureDefinitionEntityId = definition.Id,
            RoomEntityId = 99,
            RentableSpaceFurnitureEntityId = spaceFurni.Id,
        };
        FurnitureEntity item2 = new()
        {
            PlayerEntityId = player.Id,
            FurnitureDefinitionEntityId = definition.Id,
            RoomEntityId = 99,
            RentableSpaceFurnitureEntityId = spaceFurni.Id,
        };
        ctx.Furnitures.AddRange(item1, item2);
        ctx.SaveChanges();

        // Expiry: bulk-null room_id + tag for all items inside the space.
        List<FurnitureEntity> tagged = ctx
            .Furnitures.Where(f => f.RentableSpaceFurnitureEntityId == spaceFurni.Id)
            .ToList();
        foreach (FurnitureEntity f in tagged)
        {
            f.RoomEntityId = null;
            f.RentableSpaceFurnitureEntityId = null;
        }

        ctx.SaveChanges();

        List<FurnitureEntity> remaining = ctx
            .Furnitures.Where(f => f.RentableSpaceFurnitureEntityId == spaceFurni.Id)
            .ToList();

        remaining
            .Should()
            .BeEmpty("all tagged items must be untagged and returned to inventory on expiry");

        List<FurnitureEntity> returnedItems = ctx
            .Furnitures.Where(f => f.Id == item1.Id || f.Id == item2.Id)
            .ToList();
        returnedItems.Should().AllSatisfy(f => f.RoomEntityId.Should().BeNull());
    }

    [Fact]
    public void RenterPlayerIdIndex_Exists()
    {
        using TurboDbContext ctx = NewContext();

        IIndex? index = ctx
            .Model.FindEntityType(typeof(RoomRentableSpaceEntity))!
            .GetIndexes()
            .SingleOrDefault(i =>
                !i.IsUnique
                && i.Properties.Any(p =>
                    p.Name == nameof(RoomRentableSpaceEntity.RenterPlayerEntityId)
                )
            );

        index
            .Should()
            .NotBeNull(
                "renter_player_id must be indexed to efficiently enforce the one-space-per-player rule (§3.4)"
            );
    }
}
