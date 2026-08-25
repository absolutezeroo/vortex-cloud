using FluentAssertions;
using Vortex.Primitives.Observability;
using Vortex.Rooms.Wired.Engine;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The two protections that stop an "execute stacks" chain running away, which are easy to confuse
/// with each other.
/// </summary>
/// <remarks>
/// The tile set makes a cycle impossible: a pile that calls itself, or two piles that call each
/// other, would recurse until the room fell over. The depth limit is a different thing — it bounds a
/// chain that is perfectly legitimate and simply wide. Both are counted, because "why did my wired
/// stop" is otherwise unanswerable from outside the room.
/// </remarks>
public sealed class WiredCallChainGuardTests
{
    [Fact]
    public void APileIsEnteredOnce_AndTheSecondAttemptIsACycle()
    {
        FakeWiredRoomHost room = new();
        WiredCallChainGuard guard = Build(room);

        using WiredCallChainGuard.Hold first = guard.Enter(5);
        first.IsCycle.Should().BeFalse();

        using WiredCallChainGuard.Hold second = guard.Enter(5);
        second.IsCycle.Should().BeTrue("the chain is already inside that pile");

        room.StopReasons.Should().Equal([WiredStopReason.CYCLE]);
    }

    /// <summary>Releasing a hold frees the tile, so a later chain can enter it again.</summary>
    [Fact]
    public void ATileIsFreeAgainOnceItsHoldIsReleased()
    {
        WiredCallChainGuard guard = Build();

        using (WiredCallChainGuard.Hold _ = guard.Enter(5))
        {
            guard.Depth.Should().Be(1);
        }

        guard.Depth.Should().Be(0);

        using WiredCallChainGuard.Hold again = guard.Enter(5);
        again.IsCycle.Should().BeFalse();
    }

    /// <summary>
    /// A refused entry holds nothing, so disposing it must not free the tile the live hold owns.
    /// Getting this wrong turns the cycle guard off after the first cycle.
    /// </summary>
    [Fact]
    public void DisposingARefusedEntry_DoesNotReleaseTheLiveHold()
    {
        WiredCallChainGuard guard = Build();

        using WiredCallChainGuard.Hold live = guard.Enter(5);

        using (WiredCallChainGuard.Hold refused = guard.Enter(5))
        {
            refused.IsCycle.Should().BeTrue();
        }

        guard.Depth.Should().Be(1, "the live hold still owns the tile");
        guard.Enter(5).IsCycle.Should().BeTrue();
    }

    /// <summary>
    /// A negative tile index is not a tile: it is a caller saying it has none. Holding it would be
    /// holding nothing, and refusing it would stop a chain that has done nothing wrong.
    /// </summary>
    [Fact]
    public void ACallerWithNoTileHoldsNothingAndIsNotACycle()
    {
        FakeWiredRoomHost room = new();
        WiredCallChainGuard guard = Build(room);

        using WiredCallChainGuard.Hold first = guard.Enter(-1);
        using WiredCallChainGuard.Hold second = guard.Enter(-1);

        first.IsCycle.Should().BeFalse();
        second.IsCycle.Should().BeFalse();
        guard.Depth.Should().Be(0);
        room.StopReasons.Should().BeEmpty();
    }

    [Fact]
    public void AChainDescendsUntilItReachesTheDepthLimit()
    {
        FakeWiredRoomHost room = new() { MaxCallChainDepth = 3 };
        WiredCallChainGuard guard = Build(room);

        using WiredCallChainGuard.Hold a = guard.Enter(1);
        using WiredCallChainGuard.Hold b = guard.Enter(2);

        guard.HasRoomToDescend().Should().BeTrue("two deep, of three");

        using WiredCallChainGuard.Hold c = guard.Enter(3);

        guard.HasRoomToDescend().Should().BeFalse();
        room.StopReasons.Should().Equal([WiredStopReason.DEPTH]);
    }

    /// <summary>
    /// The limit is read on every check rather than captured, so an operator raising it is heard
    /// without a restart — the point of wiring WiredMaxDepth to configuration at all (RFW-101).
    /// </summary>
    [Fact]
    public void TheDepthLimitIsReadEachTime()
    {
        FakeWiredRoomHost room = new() { MaxCallChainDepth = 1 };
        WiredCallChainGuard guard = Build(room);

        using WiredCallChainGuard.Hold a = guard.Enter(1);

        guard.HasRoomToDescend().Should().BeFalse();

        room.MaxCallChainDepth = 5;

        guard
            .HasRoomToDescend()
            .Should()
            .BeTrue("the new limit applies without rebuilding anything");
    }

    private static WiredCallChainGuard Build(FakeWiredRoomHost? room = null)
    {
        FakeWiredRoomHost host = room ?? new FakeWiredRoomHost();

        return new WiredCallChainGuard(host.Diagnostics, () => host.MaxCallChainDepth);
    }
}
