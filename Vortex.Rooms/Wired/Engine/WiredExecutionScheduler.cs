using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Wired.Engine;

/// <summary>
/// The pending stack executions and when each is next due.
/// </summary>
/// <remarks>
/// <para>
/// Bookkeeping only. What a due execution actually <em>does</em> is a callback the caller supplies,
/// which is what keeps this free of the room entirely — and testable without one.
/// </para>
/// <para>
/// The subtlety is the versioning. A pending execution can be rescheduled while it already has an
/// entry sitting in the queue, and a priority queue offers no way to remove one. So a rescheduled
/// execution bumps its version and enqueues again, and the stale entry is recognised on the way out
/// by its version no longer matching. Without that, a delayed effect whose timer was reset would run
/// twice: once at its old deadline and once at its new one.
/// </para>
/// </remarks>
internal sealed class WiredExecutionScheduler
{
    private readonly Dictionary<WiredExecutionKey, WiredPendingStackExecution> _pending = [];

    // (key, version) keyed by deadline. The version is what tells a live entry from one left behind
    // by a reschedule.
    private readonly PriorityQueue<(WiredExecutionKey Key, long Version), long> _schedule = new();

    private long _nextExecutionId;

    /// <summary>How many executions are waiting, live entries and stale ones alike.</summary>
    public int PendingCount => _pending.Count;

    /// <summary>
    /// Schedules a pile's chain of actions to run at <paramref name="dueAtMs"/>, and returns the key
    /// it was filed under.
    /// </summary>
    public WiredExecutionKey Schedule(int stackId, WiredPendingStackExecution pending, long dueAtMs)
    {
        WiredExecutionKey key = new(stackId, Interlocked.Increment(ref _nextExecutionId));

        pending.Version = 1;
        pending.DueAtMs = dueAtMs;

        _pending[key] = pending;
        _schedule.Enqueue((key, pending.Version), pending.DueAtMs);

        return key;
    }

    /// <summary>
    /// Moves a pending execution to a new deadline. Bumps its version only when the deadline
    /// actually changes — re-enqueuing at the same time is a no-op that would otherwise invalidate
    /// the entry already in the queue and lose the execution.
    /// </summary>
    public void Reschedule(WiredExecutionKey key, WiredPendingStackExecution pending, long dueAtMs)
    {
        if (pending.DueAtMs != dueAtMs)
        {
            pending.Version++;
        }

        pending.DueAtMs = dueAtMs;

        _pending[key] = pending;
        _schedule.Enqueue((key, pending.Version), pending.DueAtMs);
    }

    public void Remove(WiredExecutionKey key) => _pending.Remove(key);

    /// <summary>
    /// Runs everything due at <paramref name="now"/>, up to <paramref name="budget"/> executions.
    /// </summary>
    /// <param name="execute">
    /// Runs one due execution and reports whether it is finished. False leaves it pending — a chain
    /// that has more actions to run after a delay.
    /// </param>
    /// <remarks>
    /// The budget bounds the tick's work, not the queue: what is not reached stays queued and comes
    /// back next tick, in deadline order.
    /// </remarks>
    public async Task DrainDueAsync(
        long now,
        int budget,
        Func<WiredExecutionKey, WiredPendingStackExecution, Task<bool>> execute
    )
    {
        while (budget-- > 0 && _schedule.Count > 0)
        {
            if (
                !_schedule.TryPeek(
                    out (WiredExecutionKey Key, long Version) entry,
                    out long dueAtMs
                )
            )
            {
                return;
            }

            if (dueAtMs > now)
            {
                // The queue is ordered by deadline, so the first one that is not due means none are.
                return;
            }

            _schedule.Dequeue();

            if (
                !_pending.TryGetValue(entry.Key, out WiredPendingStackExecution? pending)
                // Left behind by a reschedule, or by an execution that has already finished.
                || pending.Version != entry.Version
                // Rescheduled to later since this entry was made.
                || pending.DueAtMs > now
            )
            {
                continue;
            }

            if (await execute(entry.Key, pending))
            {
                _pending.Remove(entry.Key);
            }
        }
    }
}
