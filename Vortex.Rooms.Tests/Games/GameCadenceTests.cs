using FluentAssertions;
using Vortex.Rooms.Games.Abstractions;
using Xunit;

namespace Vortex.Rooms.Tests.Games;

/// <summary>
/// The shared sub-cadence games run their slow logic on inside the 50 ms room tick. Its semantics
/// must match what Freeze's hand-rolled <c>_nextPlayerTickMs</c> always did — the first call after a
/// reset only arms, and a stalled room does not fire a burst of catch-up ticks — because Freeze's 1s
/// freeze/shield countdown was migrated onto it with no behavior change intended.
/// </summary>
public sealed class GameCadenceTests
{
    [Fact]
    public void FirstCall_OnlyArms_ThenFiresOncePerPeriod()
    {
        GameCadence cadence = new(1000);

        cadence.Due(10_000).Should().BeFalse("the first call arms the clock, it never fires");
        cadence.Due(10_500).Should().BeFalse();
        cadence.Due(11_000).Should().BeTrue();
        cadence.Due(11_050).Should().BeFalse("it just fired; the next window starts from the fire");
        cadence.Due(12_000).Should().BeTrue();
    }

    [Fact]
    public void ALateTick_FiresOnce_AndReschedulesFromNow_NotFromTheMissedDeadline()
    {
        GameCadence cadence = new(1000);

        cadence.Due(10_000).Should().BeFalse();

        // The room stalled for three periods; exactly one fire, and the next one is a full period
        // from the late tick — no catch-up burst.
        cadence.Due(14_000).Should().BeTrue();
        cadence.Due(14_500).Should().BeFalse();
        cadence.Due(15_000).Should().BeTrue();
    }

    [Fact]
    public void Reset_RearmsInsteadOfFiring()
    {
        GameCadence cadence = new(1000);

        cadence.Due(10_000).Should().BeFalse();
        cadence.Due(11_000).Should().BeTrue();

        cadence.Reset();

        cadence.Due(20_000).Should().BeFalse("after a reset the first call arms again");
        cadence.Due(21_000).Should().BeTrue();
    }
}
