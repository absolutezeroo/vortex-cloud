using System;
using FluentAssertions;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Rooms.Configuration;
using Vortex.Rooms.Grains.Systems;
using Xunit;

namespace Vortex.Rooms.Tests.Pets;

/// <summary>
/// Age and mood, the two numbers the info panel got wrong: age was never computed at all, and the
/// happiness bar was fed the pet's nutrition because there was no mood stat to feed it.
/// </summary>
public sealed class PetAgeAndHappinessTests
{
    private static readonly DateTime Now = new(2026, 8, 3, 22, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, 1, "bought an hour ago: day one, not day zero")]
    [InlineData(1, 2, "yesterday")]
    [InlineData(30, 31, "a month old")]
    public void AgeInDays_CountsTheDayItWasCreatedAsTheFirst(
        int daysAgo,
        int expected,
        string because
    )
    {
        PetSnapshot pet = PetWith(createdAt: Now.AddDays(-daysAgo));

        pet.AgeInDays(Now).Should().Be(expected, because);
    }

    [Fact]
    public void AgeInDays_ClockSkew_NeverGoesBelowOne()
    {
        PetSnapshot pet = PetWith(createdAt: Now.AddDays(2));

        pet.AgeInDays(Now).Should().Be(1, "a pet is never zero or minus two days old");
    }

    [Fact]
    public void Happiness_DrainsWhileAwakeAndComesBackFasterWhileResting()
    {
        PetConfig config = new();

        config
            .HappinessRestGainPerMinute.Should()
            .BeGreaterThan(
                config.HappinessDecayPerMinute,
                "a nap has to be worth taking, or mood only ever falls"
            );
    }

    [Fact]
    public void Happiness_UsesItsOwnClock()
    {
        long clock = 0;
        int drained = 0;

        // Ten minutes of room ticks at the rate a waking pet loses mood.
        for (long now = 500; now <= 10 * 60_000; now += 500)
        {
            drained += RoomPetRuntime.TakeWholeNeedPoints(clock, now, 2.0, out clock);
        }

        drained.Should().Be(20, "2 a minute, the rate Habbo drains it at");
    }

    [Fact]
    public void ANewPetStartsContent()
    {
        PetWith(createdAt: Now)
            .Happiness.Should()
            .Be(100, "and so does every pet the migration touches");
    }

    private static PetSnapshot PetWith(DateTime createdAt) =>
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
            Energy = 100,
            Nutrition = 100,
            Respect = 0,
            X = 0,
            Y = 0,
            Z = 0,
            Direction = Rotation.South,
            CreatedAt = createdAt,
        };
}
