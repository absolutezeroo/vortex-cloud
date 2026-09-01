using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Engine;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// What stops a room arranging more future work than it will ever get through.
/// </summary>
/// <remarks>
/// The wired engine bounds its event queue (WiredMaxQueuedEvents) and each tick's work
/// (WiredMaxEventsPerTick, WiredMaxScheduledPerTick), and bounded nothing in between: the set of
/// delayed chains waiting to run grew without limit. Sixty-four events each arranging two delayed
/// chains, against sixty-four drained per tick, is about 1,280 net entries a second — each holding a
/// pile, its actions, the selection, the selector pool and a processing context. One room, built by
/// one player, was enough.
/// </remarks>
public sealed class WiredExecutionSchedulerBoundTests
{
    private const int CAP = 3;

    /// <summary>
    /// The scheduler is bookkeeping: it stores the object and reads its Version and DueAtMs, and
    /// never looks inside. So every required member is a stub — filling them in would be describing
    /// a chain nothing here runs.
    /// </summary>
    private static WiredPendingStackExecution Pending() =>
        new()
        {
            Stack = FakeProxy.Create<IWiredStack>(_ => null),
            Actions = [],
            Trigger = FakeProxy.Create<IWiredTrigger>(_ => null),
            Policy = FakeProxy.Create<IWiredPolicy>(_ => null),
            Selected = FakeProxy.Create<IWiredSelectionSet>(_ => null),
            SelectorPool = FakeProxy.Create<IWiredSelectionSet>(_ => null),
            Signal = FakeProxy.Create<IWiredSelectionSet>(_ => null),
            ProcessingContext = FakeProxy.Create<IWiredProcessingContext>(_ => null),
            NextActionIndex = 0,
        };

    /// <summary>The room's own loop: refuse when full, otherwise file it.</summary>
    private static int ScheduleMany(WiredExecutionScheduler scheduler, int count, long dueAtMs)
    {
        int refused = 0;

        for (int i = 0; i < count; i++)
        {
            if (scheduler.IsFull(CAP))
            {
                refused++;

                continue;
            }

            scheduler.Schedule(i, Pending(), dueAtMs);
        }

        return refused;
    }

    [Fact]
    public void SchedulingPastTheCap_IsRefusedRatherThanAccumulated()
    {
        WiredExecutionScheduler scheduler = new();

        int refused = ScheduleMany(scheduler, CAP + 5, 1_000);

        refused.Should().Be(5, "everything past the cap is turned away");
        scheduler.PendingCount.Should().Be(CAP, "and nothing past it is held");
    }

    /// <summary>
    /// The cap is on what is waiting, not on what a room may ever schedule: draining frees the room
    /// back up. Without this the first busy minute would leave a room unable to run wired again.
    /// </summary>
    [Fact]
    public async Task DrainingReleasesTheRoomForMoreWork()
    {
        WiredExecutionScheduler scheduler = new();

        ScheduleMany(scheduler, CAP, 1_000);

        await scheduler
            .DrainDueAsync(1_000, CAP, (_, _) => Task.FromResult(true))
            .ConfigureAwait(true);

        scheduler.PendingCount.Should().Be(0);
        scheduler.IsFull(CAP).Should().BeFalse("the room can arrange work again");
    }
}
