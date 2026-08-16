using FluentAssertions;
using Vortex.Rooms.Wired;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The two add-ons that decide how often and how much of a pile runs: "execution limit" (N firings
/// per window) and "unseen effect" (one effect that has not run yet). Both were registered and
/// inert, which for a limit means no limit and for a cycle means the whole pile every time.
/// </summary>
public sealed class WiredEffectPacingTests
{
    [Fact]
    public void TheLimitAllowsItsQuota_ThenRefuses()
    {
        WiredExecutionWindow window = new();

        window.TryConsume(2, 1_000, 0).Should().BeTrue();
        window.TryConsume(2, 1_000, 100).Should().BeTrue();
        window.TryConsume(2, 1_000, 200).Should().BeFalse();
    }

    [Fact]
    public void TheWindowRolls_RatherThanExpiringAsABucket()
    {
        WiredExecutionWindow window = new();

        window.TryConsume(2, 1_000, 0).Should().BeTrue();
        window.TryConsume(2, 1_000, 500).Should().BeTrue();
        window.TryConsume(2, 1_000, 900).Should().BeFalse();

        // The first firing has aged out at 1000, so one slot frees up there — not at some period
        // boundary.
        window.TryConsume(2, 1_000, 1_000).Should().BeTrue();
        window.TryConsume(2, 1_000, 1_100).Should().BeFalse();
    }

    [Fact]
    public void NoLimitConfigured_NeverRefuses()
    {
        WiredExecutionWindow window = new();

        for (int i = 0; i < 20; i++)
        {
            window.TryConsume(0, 0, i).Should().BeTrue();
        }
    }

    [Fact]
    public void TheUnseenCycle_GoesThroughEveryEffectBeforeRepeating()
    {
        WiredUnseenCycle cycle = new();
        int[] effects = [10, 20, 30];

        cycle.Next(effects).Should().Be(0);
        cycle.Next(effects).Should().Be(1);
        cycle.Next(effects).Should().Be(2);
    }

    [Fact]
    public void OnceAllAreSeen_TheCycleStartsOver()
    {
        // Not starting over would leave the pile permanently silent, which reads in-game as broken
        // wiring rather than as a finished cycle.
        WiredUnseenCycle cycle = new();
        int[] effects = [10, 20];

        cycle.Next(effects);
        cycle.Next(effects);

        cycle.Next(effects).Should().Be(0);
        cycle.Next(effects).Should().Be(1);
    }

    [Fact]
    public void AnEffectAddedMidCycle_IsTakenNext()
    {
        WiredUnseenCycle cycle = new();

        cycle.Next([10, 20]).Should().Be(0);

        // The builder drops another effect on the pile: it has not been seen, so it is next.
        cycle.Next([10, 20, 30]).Should().Be(1);
        cycle.Next([10, 20, 30]).Should().Be(2);
    }

    [Fact]
    public void APileWithNoEffects_HasNothingToRun()
    {
        new WiredUnseenCycle().Next([]).Should().Be(-1);
    }
}
