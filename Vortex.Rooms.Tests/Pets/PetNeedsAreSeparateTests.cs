using FluentAssertions;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Rooms.Configuration;
using Vortex.Rooms.Grains.Systems;
using Xunit;

namespace Vortex.Rooms.Tests.Pets;

/// <summary>
/// Habbo counts four needs -- hunger, thirst, energy and happiness -- and the pet panel names them
/// separately. Thirst used to be read off the energy bar, so one number answered both "wants a
/// drink" and "wants a nap": drinking cancelled the pet's need to sleep, and sleeping cancelled its
/// thirst.
/// </summary>
public sealed class PetNeedsAreSeparateTests
{
    [Fact]
    public void APetCanBeThirstyWithoutBeingTired()
    {
        PetSnapshot pet = PetWith(energy: 100, thirst: 10);
        PetTuning tuning = PetTuning.FromDefaults(new PetConfig());

        bool thirsty = pet.Thirst < tuning.ThirstThreshold;
        bool tired = RoomPetRuntime.IsTired(pet, tuning.TiredEnergyThreshold);

        thirsty.Should().BeTrue();
        tired.Should().BeFalse("a full energy bar is not a full water bowl");
    }

    [Fact]
    public void APetCanBeTiredWithoutBeingThirsty()
    {
        PetSnapshot pet = PetWith(energy: 5, thirst: 100);
        PetTuning tuning = PetTuning.FromDefaults(new PetConfig());

        bool thirsty = pet.Thirst < tuning.ThirstThreshold;
        bool tired = RoomPetRuntime.IsTired(pet, tuning.TiredEnergyThreshold);

        thirsty.Should().BeFalse();
        tired.Should().BeTrue("a nap is not a drink");
    }

    [Fact]
    public void EveryNeedRunsOnItsOwnRate()
    {
        PetConfig config = new();

        double[] rates =
        [
            config.NutritionDecayPerMinute,
            config.EnergyDecayPerMinute,
            config.ThirstDecayPerMinute,
            config.HappinessDecayPerMinute,
        ];

        rates.Should().OnlyContain(r => r > 0, "a need that never drains is not a need");
    }

    [Fact]
    public void ANewPetStartsSlaked()
    {
        PetWith(energy: 100, thirst: 100)
            .Thirst.Should()
            .Be(100, "and so does every pet the migration touches");
    }

    private static PetSnapshot PetWith(int energy, int thirst) =>
        new()
        {
            PetId = 1,
            OwnerId = new PlayerId(1),
            RoomId = 10,
            Name = "Rex",
            Type = 0,
            Race = 0,
            Color = "FFFFFF",
            Gender = AvatarGenderType.Male,
            Level = 1,
            Experience = 0,
            Energy = energy,
            Nutrition = 100,
            Thirst = thirst,
            Respect = 0,
            X = 0,
            Y = 0,
            Z = 0,
            Direction = Rotation.South,
        };
}
