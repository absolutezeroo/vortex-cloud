using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Turbo.Database.Context;
using Turbo.Database.Entities.Pets;
using Turbo.Primitives.Rooms.Enums;
using Xunit;

namespace Turbo.Database.Tests.Pets;

public sealed class PetModelTests
{
    [Fact]
    public void PetEntity_HasInventoryPlacementIndexesAndDefaults()
    {
        using TurboDbContext context = NewContext();

        IEntityType pet = context.Model.FindEntityType(typeof(PetEntity))!;

        pet.FindProperty(nameof(PetEntity.RoomEntityId))!
            .IsNullable.Should()
            .BeTrue("room_id is nullable so null means inventory");

        pet.GetIndexes()
            .Should()
            .Contain(i => i.Properties.Any(p => p.Name == nameof(PetEntity.OwnerPlayerEntityId)));
        pet.GetIndexes()
            .Should()
            .Contain(i => i.Properties.Any(p => p.Name == nameof(PetEntity.RoomEntityId)));

        pet.FindProperty(nameof(PetEntity.Gender))!
            .GetDefaultValue()
            .Should()
            .Be(AvatarGenderType.Male);
        pet.FindProperty(nameof(PetEntity.Level))!.GetDefaultValue().Should().Be(1);
        pet.FindProperty(nameof(PetEntity.Experience))!.GetDefaultValue().Should().Be(0);
        pet.FindProperty(nameof(PetEntity.Respect))!.GetDefaultValue().Should().Be(0);
    }

    [Fact]
    public void PetFoodEntity_HasDefinitionUniquenessAndPetTypeIndex()
    {
        using TurboDbContext context = NewContext();

        IEntityType petFood = context.Model.FindEntityType(typeof(PetFoodEntity))!;

        petFood
            .GetIndexes()
            .Should()
            .Contain(i =>
                i.IsUnique
                && i.Properties.Any(p =>
                    p.Name == nameof(PetFoodEntity.FurnitureDefinitionEntityId)
                )
            );
        petFood
            .GetIndexes()
            .Should()
            .Contain(i => i.Properties.Any(p => p.Name == nameof(PetFoodEntity.PetType)));
    }

    private static TurboDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<TurboDbContext>()
                .UseInMemoryDatabase($"pet-model-{Guid.NewGuid():N}")
                .Options
        );
}
