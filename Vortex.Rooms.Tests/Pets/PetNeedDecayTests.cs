using FluentAssertions;
using Vortex.Rooms.Grains.Systems;
using Xunit;

namespace Vortex.Rooms.Tests.Pets;

/// <summary>
/// Nutrition and energy decay on separate clocks. They used to share one, reset to "now" whenever
/// either need lost a whole point, and the faster need starved the slower one: nutrition at 1.0/min
/// reset the accumulator every minute, so energy at 0.5/min never reached a whole point. Pets never
/// tired, never got thirsty and never fell asleep.
/// </summary>
public sealed class PetNeedDecayTests
{
    private const double NutritionPerMinute = 1.0;
    private const double EnergyPerMinute = 0.5;
    private const int TickMs = 500;

    [Fact]
    public void NeedClocks_TickedTogetherAtRoomRate_DecayBothAtTheirOwnRate()
    {
        long nutritionClock = 0;
        long energyClock = 0;
        int nutritionLost = 0;
        int energyLost = 0;

        // Ten minutes of room ticks, the same 500ms cadence RoomPetSystem runs at.
        for (long now = TickMs; now <= 10 * 60_000; now += TickMs)
        {
            nutritionLost += RoomPetRuntime.TakeWholeNeedPoints(
                nutritionClock,
                now,
                NutritionPerMinute,
                out nutritionClock
            );
            energyLost += RoomPetRuntime.TakeWholeNeedPoints(
                energyClock,
                now,
                EnergyPerMinute,
                out energyClock
            );
        }

        nutritionLost.Should().Be(10, "1.0/min over ten minutes");
        energyLost.Should().Be(5, "0.5/min over ten minutes -- the slower need must still decay");
    }

    [Fact]
    public void TakeWholeNeedPoints_BelowOneWholePoint_LeavesTheClockUntouched()
    {
        long clock = 0;

        int points = RoomPetRuntime.TakeWholeNeedPoints(clock, 59_000, EnergyPerMinute, out clock);

        points.Should().Be(0);
        clock.Should().Be(0, "the fraction carries rather than being thrown away");
    }

    [Fact]
    public void TakeWholeNeedPoints_WholePointTaken_CarriesTheRemainder()
    {
        long clock = 0;

        int points = RoomPetRuntime.TakeWholeNeedPoints(clock, 150_000, EnergyPerMinute, out clock);

        points.Should().Be(1, "0.5/min reaches one whole point at two minutes");
        clock.Should().Be(120_000, "the unspent 30s stays on the clock, it is not rounded away");
    }

    [Fact]
    public void TakeWholeNeedPoints_ZeroRate_NeverAccrues()
    {
        long clock = 0;

        int points = RoomPetRuntime.TakeWholeNeedPoints(clock, 600_000, 0, out clock);

        points.Should().Be(0);
        clock.Should().Be(600_000, "a disabled need must not bank time it would spend later");
    }
}
