using System;
using FluentAssertions;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Rooms.Grains.Systems;
using Xunit;

namespace Vortex.Rooms.Tests.Pets;

/// <summary>
/// A monsterplant used to be an immortal decoration: the pet tick skipped type 16 outright, so it
/// never dried out and never grew, which left every button the client offers it — treat, harvest,
/// revive, compost — pointing at nothing.
/// <para>
/// These cover the two functions that decide the whole cycle. Both are derived rather than
/// accumulated, which is what makes a plant survive a room reload: well-being from the watering
/// stamp the client counts down from, growth from the seconds banked in the pet's experience.
/// </para>
/// </summary>
public sealed class MonsterplantLifecycleTests
{
    private const int MonsterplantType = 16;
    private const int MaxLevel = 7;
    private const int EnergyCap = 100;

    /// <summary>The client is told the plant has 24 hours; the server has to mean the same 24 hours.</summary>
    private const int WellBeingSeconds = 86_400;

    private const int GrowthSeconds = 43_200;

    private static readonly DateTime Noon = new(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AFreshlyWateredPlant_IsAtFullWellBeing() =>
        RoomPetRuntime
            .PlantWellBeing(Noon, Noon, EnergyCap, WellBeingSeconds)
            .Should()
            .Be(EnergyCap);

    [Fact]
    public void HalfwayThroughTheWindow_ItIsHalfWay() =>
        RoomPetRuntime
            .PlantWellBeing(Noon, Noon.AddHours(12), EnergyCap, WellBeingSeconds)
            .Should()
            .Be(50);

    [Fact]
    public void AtTheEndOfTheWindow_ItIsDead() =>
        RoomPetRuntime
            .PlantWellBeing(Noon, Noon.AddSeconds(WellBeingSeconds), EnergyCap, WellBeingSeconds)
            .Should()
            .Be(0);

    [Fact]
    public void LongAfterTheWindow_ItStaysDeadRatherThanGoingNegative() =>
        // A room can sit unloaded for days; the derived value has to clamp rather than wrap.
        RoomPetRuntime
            .PlantWellBeing(Noon, Noon.AddDays(30), EnergyCap, WellBeingSeconds)
            .Should()
            .Be(0);

    [Fact]
    public void ADeadPlantIsWhatTheClientCallsRevivable() =>
        RoomPetRuntime.CanRevive(Plant(energy: 0)).Should().BeTrue();

    [Theory]
    [InlineData(0, 1)]
    [InlineData(GrowthSeconds - 1, 1)]
    [InlineData(GrowthSeconds, 2)]
    [InlineData(GrowthSeconds * 5, 6)]
    [InlineData(GrowthSeconds * 6, MaxLevel)]
    public void GrowthStagesFollowTheBankedSeconds(int grownSeconds, int expected) =>
        RoomPetRuntime.PlantLevelFor(grownSeconds, GrowthSeconds, MaxLevel).Should().Be(expected);

    [Fact]
    public void GrowthStopsAtFullSize() =>
        // Otherwise a plant left alone for a month would report a stage the client has no art for.
        RoomPetRuntime
            .PlantLevelFor(GrowthSeconds * 100, GrowthSeconds, MaxLevel)
            .Should()
            .Be(MaxLevel);

    [Fact]
    public void AFullyGrownPlantWithItsChargeOffersHarvest() =>
        RoomPetRuntime.CanHarvest(Plant(level: MaxLevel, canBreed: true)).Should().BeTrue();

    [Fact]
    public void OnceHarvested_TheButtonGoesAwayUntilARebreedPotion() =>
        // The charge is what the rebreed potion restores ("your plant can produce new seeds"), so a
        // spent plant must stop offering harvest rather than offering one that does nothing.
        RoomPetRuntime.CanHarvest(Plant(level: MaxLevel, canBreed: false)).Should().BeFalse();

    [Fact]
    public void AWitheredPlantOffersNoHarvest_HoweverGrownItWas() =>
        RoomPetRuntime
            .CanHarvest(Plant(level: MaxLevel, canBreed: true, energy: 0))
            .Should()
            .BeFalse();

    [Fact]
    public void TreatingAndRespectingShareOneWire() =>
        // The client sends the same composer for both buttons (_composers[576] = _SafeCls_1909, used
        // by givePetRespect and by RWUAM_TREAT_PET), so the split has to happen on the pet's type,
        // not on the header. Splitting it into two headers is what this pins against -- the second
        // one collides on registration and takes the whole revision map down.
        RoomPetRuntime.IsPlant(Plant()).Should().BeTrue();

    [Fact]
    public void AnAnimalIsNotTreatedByThatSameWire() =>
        RoomPetRuntime.IsPlant(Plant() with { Type = 0 }).Should().BeFalse();

    private static PetSnapshot Plant(int energy = 100, int level = 1, bool canBreed = true) =>
        new()
        {
            PetId = 1,
            OwnerId = new PlayerId(1),
            RoomId = 10,
            Name = "Monsterplant",
            Type = MonsterplantType,
            Race = 0,
            Color = "FFFFFF",
            Gender = AvatarGenderType.Male,
            Level = level,
            Experience = 0,
            Energy = energy,
            Nutrition = 100,
            Respect = 0,
            X = 0,
            Y = 0,
            Z = 0,
            Direction = Rotation.South,
            CanBreed = canBreed,
        };
}
