using FluentAssertions;
using Vortex.Rooms.Wired;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// Locks the one-shot cadence of the wired "trigger once" trigger (wf_trg_at_given_time): it arms on
/// its first poll, fires exactly once one delay later, and never fires again — the guarantee the room
/// tick's timed-trigger producer relies on so a one-shot box does not repeat.
/// </summary>
public sealed class WiredOneShotScheduleTests
{
    [Fact]
    public void ArmsOnFirstPoll_DelayMeasuredFromThere()
    {
        WiredOneShotSchedule schedule = new(200);

        schedule.TryConsumeDue(1_000).Should().BeFalse(); // first poll arms fire at 1_200
        schedule.TryConsumeDue(1_199).Should().BeFalse(); // one ms early
        schedule.TryConsumeDue(1_200).Should().BeTrue(); // exactly the delay boundary
    }

    [Fact]
    public void FiresExactlyOnce_ThenNeverAgain()
    {
        WiredOneShotSchedule schedule = new(1_000);

        schedule.TryConsumeDue(5_000).Should().BeFalse(); // arms fire at 6_000
        schedule.TryConsumeDue(6_000).Should().BeTrue(); // the single firing
        schedule.TryConsumeDue(6_000).Should().BeFalse(); // same tick, already fired
        schedule.TryConsumeDue(7_000).Should().BeFalse();
        schedule.TryConsumeDue(9_999_999).Should().BeFalse(); // never again
    }

    [Fact]
    public void LatePoll_StillFiresOnce()
    {
        // If the room was busy and the first poll after arming is well past the fire time, it still
        // fires (once), rather than being skipped.
        WiredOneShotSchedule schedule = new(500);

        schedule.TryConsumeDue(0).Should().BeFalse(); // arms fire at 500
        schedule.TryConsumeDue(10_000).Should().BeTrue(); // long overdue → fires
        schedule.TryConsumeDue(10_001).Should().BeFalse();
    }
}
