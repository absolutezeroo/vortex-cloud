using FluentAssertions;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Rooms.Configuration;
using Vortex.Rooms.Grains.Systems;
using Xunit;

namespace Vortex.Rooms.Tests.Pets;

/// <summary>
/// A tired pet walks to its nest instead of dropping where it stands. Covers the two decisions the
/// walk is built on; the pathing itself belongs to the room tick and is checked in game.
/// </summary>
public sealed class PetNestSeekingTests
{
    private const int MonsterplantType = 16;
    private const int DogType = 0;

    [Theory]
    [InlineData(100, false)]
    [InlineData(21, false)]
    [InlineData(20, true)]
    [InlineData(1, true)]
    [InlineData(0, true)]
    public void IsTired_CrossingTheThreshold_SendsThePetToBed(int energy, bool expected)
    {
        RoomPetRuntime
            .IsTired(PetWith(DogType, energy), tiredEnergyThreshold: 20)
            .Should()
            .Be(expected);
    }

    [Fact]
    public void IsTired_Monsterplant_NeverSeeksANest()
    {
        RoomPetRuntime
            .IsTired(PetWith(MonsterplantType, energy: 0), tiredEnergyThreshold: 20)
            .Should()
            .BeFalse("a monsterplant is rooted -- its energy means watering, not sleep");
    }

    [Fact]
    public void PickNearestTile_SeveralNests_TakesTheClosest()
    {
        (int X, int Y)? nest = RoomPetRuntime.PickNearestTile(
            fromX: 1,
            fromY: 1,
            [(9, 9), (3, 2), (0, 8)]
        );

        nest.Should().Be((3, 2));
    }

    [Fact]
    public void PickNearestTile_EquidistantNests_KeepsTheFirst()
    {
        (int X, int Y)? nest = RoomPetRuntime.PickNearestTile(fromX: 0, fromY: 0, [(2, 0), (0, 2)]);

        nest.Should().Be((2, 0), "a stable pick stops the pet dithering between two nests");
    }

    [Fact]
    public void PickNearestTile_NoNestInTheRoom_ReturnsNull()
    {
        RoomPetRuntime.PickNearestTile(fromX: 4, fromY: 4, []).Should().BeNull();
    }

    [Fact]
    public void PetConfig_TiredThreshold_StaysBelowTheWakeThreshold()
    {
        PetConfig config = new();

        config
            .TiredEnergyThreshold.Should()
            .BeLessThan(
                config.SleepWakeEnergyThreshold,
                "a pet that naps at the wake level would stand back up on the same tick"
            );
    }

    private static PetSnapshot PetWith(int type, int energy) =>
        new()
        {
            PetId = 1,
            OwnerId = new PlayerId(1),
            RoomId = 10,
            Name = "Rex",
            Type = type,
            Race = 0,
            Color = "FFFFFF",
            Gender = AvatarGenderType.Male,
            Level = 1,
            Experience = 0,
            Energy = energy,
            Nutrition = 100,
            Respect = 0,
            X = 0,
            Y = 0,
            Z = 0,
            Direction = Rotation.South,
        };
}
