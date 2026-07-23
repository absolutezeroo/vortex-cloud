using FluentAssertions;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Rooms.Wired;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// Locks the firing cadence of periodic wired triggers (<c>wf_trg_period_short</c> /
/// <c>wf_trg_period_long</c>): how the configured delay knob maps to a room-clock interval, that
/// a freshly loaded box fires immediately, and that it then re-arms exactly one interval at a
/// time (no drift, no double-fire). This is the contract the room tick's periodic producer relies
/// on to decide which boxes are due.
/// </summary>
public sealed class WiredPeriodicScheduleTests
{
    [Theory]
    [InlineData(1, 50)]
    [InlineData(2, 100)]
    [InlineData(10, 500)]
    [InlineData(0, 50)] // clamped up to the minimum knob (1)
    [InlineData(25, 500)] // clamped down to the maximum knob (10)
    public void Short_DelayValue_MapsTo_FiftyMsSteps(int delayValue, int expectedMs)
    {
        WiredPeriodicSchedule schedule = new(50, 10) { DelayValue = delayValue };

        schedule.DelayMs.Should().Be(expectedMs);
    }

    [Theory]
    [InlineData(1, 5_000)]
    [InlineData(2, 10_000)]
    [InlineData(120, 600_000)]
    [InlineData(0, 5_000)] // clamped up to the minimum knob (1)
    [InlineData(500, 600_000)] // clamped down to the maximum knob (120)
    public void Long_DelayValue_MapsTo_FiveSecondSteps(int delayValue, int expectedMs)
    {
        WiredPeriodicSchedule schedule = new(5000, 120) { DelayValue = delayValue };

        schedule.DelayMs.Should().Be(expectedMs);
    }

    [Fact]
    public void FreshlyLoaded_Schedule_IsImmediatelyDue()
    {
        // A box that was just placed / reloaded has never been advanced, so it must fire on the
        // very next wired tick rather than waiting a full interval first.
        WiredPeriodicSchedule schedule = new(50, 10) { DelayValue = 4 };

        schedule.IsDue(0).Should().BeTrue();
        schedule.IsDue(123_456).Should().BeTrue();
    }

    [Fact]
    public void After_Advance_NotDue_Until_ExactlyOneInterval_Later()
    {
        WiredPeriodicSchedule schedule = new(50, 10) { DelayValue = 4 };
        // DelayMs = 4 * 50 = 200ms.

        schedule.Advance(1_000);

        schedule.IsDue(1_199).Should().BeFalse(); // one ms early
        schedule.IsDue(1_200).Should().BeTrue(); // exactly the interval boundary
        schedule.IsDue(5_000).Should().BeTrue(); // and any time after
    }

    [Fact]
    public void TryConsumeDue_FiresWhenDue_AndRearms()
    {
        WiredPeriodicSchedule schedule = new(50, 10) { DelayValue = 2 };
        // DelayMs = 2 * 50 = 100ms.

        schedule.TryConsumeDue(1_000).Should().BeTrue(); // immediately due, re-arms to 1_100
        schedule.TryConsumeDue(1_099).Should().BeFalse();
        schedule.TryConsumeDue(1_100).Should().BeTrue(); // re-armed interval elapsed
        schedule.TryConsumeDue(1_100).Should().BeFalse(); // same tick, already consumed
    }

    [Fact]
    public void Rearming_From_ActualFireTime_KeepsFixedInterval()
    {
        // Mirrors the producer loop: fire at `now`, advance from `now`, repeat. Each firing is one
        // full interval after the previous fire time.
        WiredPeriodicSchedule schedule = new(5000, 120) { DelayValue = 1 };
        // DelayMs = 5000ms.

        long now = 10_000;

        schedule.IsDue(now).Should().BeTrue();
        schedule.Advance(now);

        schedule.IsDue(now + 4_999).Should().BeFalse();

        now += 5_000;
        schedule.IsDue(now).Should().BeTrue();
        schedule.Advance(now);

        schedule.IsDue(now + 1).Should().BeFalse();
        schedule.IsDue(now + 5_000).Should().BeTrue();
    }
}
