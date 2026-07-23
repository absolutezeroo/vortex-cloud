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

    [Fact]
    public void Reset_ReArms_SoItCanFireAgain()
    {
        // The Timer Reset effect (wf_act_reset_timers) re-arms an "at given time" box: the delay restarts
        // from the next poll and it fires once more. Without a reset it stays fired forever.
        WiredOneShotSchedule schedule = new(200);

        schedule.TryConsumeDue(1_000).Should().BeFalse(); // arms fire at 1_200
        schedule.TryConsumeDue(1_200).Should().BeTrue(); // the single firing
        schedule.TryConsumeDue(5_000).Should().BeFalse(); // still spent

        schedule.Reset();

        schedule.TryConsumeDue(5_000).Should().BeFalse(); // re-arms from this poll → fires at 5_200
        schedule.TryConsumeDue(5_199).Should().BeFalse();
        schedule.TryConsumeDue(5_200).Should().BeTrue(); // fires again after the reset
        schedule.TryConsumeDue(9_999).Should().BeFalse(); // and is spent again
    }

    [Fact]
    public void DelayMs_ExposesTheConfiguredDelay()
    {
        // The trigger compares this against the freshly-parsed config to decide whether a reload is a
        // genuine reconfigure (re-arm) or just the pile being resolved again (must NOT re-arm, or the
        // one-shot would fire over and over).
        new WiredOneShotSchedule(3_500)
            .DelayMs.Should()
            .Be(3_500);
    }
}
