using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Engine;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// When a pile's delayed chain runs, and what happens when its deadline moves under it.
/// </summary>
/// <remarks>
/// The versioning is the whole point of this class. A pending execution can be rescheduled while it
/// already has an entry in the priority queue, and a priority queue offers no way to take one out —
/// so the stale entry is recognised on the way out by its version. Get that wrong and a delayed
/// effect whose timer was reset runs twice: once at its old deadline, once at its new one.
/// </remarks>
public sealed class WiredExecutionSchedulerTests
{
    private const int STACK = 3;

    [Fact]
    public async Task NothingRunsBeforeItIsDue()
    {
        WiredExecutionScheduler scheduler = new();
        List<WiredExecutionKey> ran = [];

        scheduler.Schedule(STACK, Pending(), dueAtMs: 1_000);

        await scheduler.DrainDueAsync(999, budget: 10, Finish(ran));

        ran.Should().BeEmpty();

        await scheduler.DrainDueAsync(1_000, budget: 10, Finish(ran));

        ran.Should().ContainSingle("due means due at, not due after");
    }

    [Fact]
    public async Task DueExecutionsRunInDeadlineOrder()
    {
        WiredExecutionScheduler scheduler = new();
        List<WiredExecutionKey> ran = [];

        WiredExecutionKey late = scheduler.Schedule(STACK, Pending(), dueAtMs: 300);
        WiredExecutionKey early = scheduler.Schedule(STACK + 1, Pending(), dueAtMs: 100);
        WiredExecutionKey middle = scheduler.Schedule(STACK + 2, Pending(), dueAtMs: 200);

        await scheduler.DrainDueAsync(1_000, budget: 10, Finish(ran));

        ran.Should().Equal([early, middle, late]);
    }

    /// <summary>
    /// The budget bounds the tick's work, not the queue. What is not reached stays queued and comes
    /// back on the next tick — still in deadline order.
    /// </summary>
    [Fact]
    public async Task TheBudgetBoundsTheTickAndTheRestComesBack()
    {
        WiredExecutionScheduler scheduler = new();
        List<WiredExecutionKey> ran = [];

        WiredExecutionKey first = scheduler.Schedule(STACK, Pending(), dueAtMs: 100);
        WiredExecutionKey second = scheduler.Schedule(STACK + 1, Pending(), dueAtMs: 200);

        await scheduler.DrainDueAsync(1_000, budget: 1, Finish(ran));

        ran.Should().Equal([first]);

        await scheduler.DrainDueAsync(1_000, budget: 10, Finish(ran));

        ran.Should().Equal([first, second]);
    }

    /// <summary>
    /// The one that matters. Rescheduling leaves a stale entry in the queue that cannot be removed;
    /// running it as well as the live one would fire a reset timer's effects twice.
    /// </summary>
    [Fact]
    public async Task ARescheduledExecutionRunsOnceAtItsNewDeadline()
    {
        WiredExecutionScheduler scheduler = new();
        List<WiredExecutionKey> ran = [];

        WiredPendingStackExecution pending = Pending();
        WiredExecutionKey key = scheduler.Schedule(STACK, pending, dueAtMs: 100);

        scheduler.Reschedule(key, pending, dueAtMs: 500);

        // Past the old deadline, before the new one: the stale entry is there and must not run.
        await scheduler.DrainDueAsync(200, budget: 10, Finish(ran));

        ran.Should().BeEmpty("the entry left behind by the reschedule is not the live one");

        await scheduler.DrainDueAsync(500, budget: 10, Finish(ran));

        ran.Should().ContainSingle().Which.Should().Be(key);
    }

    /// <summary>
    /// Rescheduling to the same deadline must not bump the version: doing so would invalidate the
    /// entry already in the queue and there would be nothing live left to run it.
    /// </summary>
    [Fact]
    public async Task ReschedulingToTheSameDeadlineDoesNotLoseTheExecution()
    {
        WiredExecutionScheduler scheduler = new();
        List<WiredExecutionKey> ran = [];

        WiredPendingStackExecution pending = Pending();
        WiredExecutionKey key = scheduler.Schedule(STACK, pending, dueAtMs: 100);

        scheduler.Reschedule(key, pending, dueAtMs: 100);

        await scheduler.DrainDueAsync(100, budget: 10, Finish(ran));

        ran.Should().ContainSingle().Which.Should().Be(key);
    }

    /// <summary>
    /// A chain with more actions to run after a delay stays pending: the callback saying "not
    /// finished" is what keeps it alive across ticks.
    /// </summary>
    [Fact]
    public async Task AnUnfinishedChainStaysPending()
    {
        WiredExecutionScheduler scheduler = new();

        WiredPendingStackExecution pending = Pending();
        WiredExecutionKey key = scheduler.Schedule(STACK, pending, dueAtMs: 100);

        await scheduler.DrainDueAsync(
            100,
            budget: 10,
            (k, p) =>
            {
                // What a delayed action does: push itself out and report unfinished.
                scheduler.Reschedule(k, p, dueAtMs: 400);

                return Task.FromResult(false);
            }
        );

        scheduler.PendingCount.Should().Be(1);

        List<WiredExecutionKey> ran = [];
        await scheduler.DrainDueAsync(400, budget: 10, Finish(ran));

        ran.Should().ContainSingle().Which.Should().Be(key);
        scheduler.PendingCount.Should().Be(0, "and once it reports finished it is gone");
    }

    [Fact]
    public async Task AnExecutionRemovedBeforeItsDeadline_NeverRuns()
    {
        WiredExecutionScheduler scheduler = new();
        List<WiredExecutionKey> ran = [];

        WiredExecutionKey key = scheduler.Schedule(STACK, Pending(), dueAtMs: 100);

        scheduler.Remove(key);

        await scheduler.DrainDueAsync(1_000, budget: 10, Finish(ran));

        ran.Should().BeEmpty();
    }

    /// <summary>Two chains on the same pile are two executions, not one overwriting the other.</summary>
    [Fact]
    public async Task TwoChainsOnOnePileAreBothScheduled()
    {
        WiredExecutionScheduler scheduler = new();
        List<WiredExecutionKey> ran = [];

        WiredExecutionKey first = scheduler.Schedule(STACK, Pending(), dueAtMs: 100);
        WiredExecutionKey second = scheduler.Schedule(STACK, Pending(), dueAtMs: 100);

        first.Should().NotBe(second);

        await scheduler.DrainDueAsync(1_000, budget: 10, Finish(ran));

        ran.Should().HaveCount(2);
    }

    [Fact]
    public async Task AnEmptyScheduleDrainsToNothing()
    {
        WiredExecutionScheduler scheduler = new();
        List<WiredExecutionKey> ran = [];

        await scheduler.DrainDueAsync(1_000, budget: 10, Finish(ran));

        ran.Should().BeEmpty();
        scheduler.PendingCount.Should().Be(0);
    }

    private static Func<WiredExecutionKey, WiredPendingStackExecution, Task<bool>> Finish(
        List<WiredExecutionKey> ran
    ) =>
        (key, _) =>
        {
            ran.Add(key);

            return Task.FromResult(true);
        };

    /// <summary>A pending execution with nothing in it — the scheduler never looks inside.</summary>
    private static WiredPendingStackExecution Pending() =>
        new()
        {
            Stack = new WiredStack { StackId = STACK },
            Actions = [],
            Trigger = null,
            Policy = new WiredPolicy(),
            Selected = new WiredSelectionSet(),
            SelectorPool = new WiredSelectionSet(),
            Signal = new WiredSelectionSet(),
            ProcessingContext = null!,
            NextActionIndex = 0,
        };
}
