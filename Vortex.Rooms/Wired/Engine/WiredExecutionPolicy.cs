using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired;

namespace Vortex.Rooms.Wired.Engine;

/// <summary>
/// How often a pile may fire, and which of its effects run when it does.
/// </summary>
/// <remarks>
/// <para>
/// All of this is per-pile runtime state that deliberately does not survive a room unload: an
/// allowance window, an unseen cycle, and what the random add-on last drew. It lives here rather
/// than on the add-on boxes because a box is rebuilt from its tile on every fire and would forget
/// between them.
/// </para>
/// <para>
/// The random draw takes its <see cref="Random"/> from the caller. A pile that picks "two effects,
/// avoiding the last three firings" has behaviour worth pinning, and pinning it needs a sequence a
/// test can predict — <c>Random.Shared</c> made that impossible.
/// </para>
/// </remarks>
internal sealed class WiredExecutionPolicy(IWiredDiagnostics diagnostics, Random random)
{
    private readonly IWiredDiagnostics _diagnostics = diagnostics;
    private readonly Random _random = random;

    // Keyed by stack (tile) id, and ephemeral: a room that unloads starts its cycles and its windows
    // again.
    private readonly Dictionary<int, WiredExecutionWindow> _windows = [];
    private readonly Dictionary<int, WiredUnseenCycle> _unseenCycles = [];
    private readonly Dictionary<int, Queue<HashSet<int>>> _recentRandomPicks = [];

    /// <summary>
    /// Whether this pile may fire now, against the execution-limit add-on's "N times per window".
    /// Records the firing when it may.
    /// </summary>
    /// <remarks>
    /// The window is rolling rather than fixed: firings older than it are forgotten as we go, so a
    /// pile limited to 3 per 5 seconds can always fire again 5 seconds after its third — it does not
    /// wait for a bucket to expire. Habbo's own semantics here are <c>UNKNOWN</c> (OQ-6); this is
    /// Vortex's documented choice, and the test that pins it is what makes it a choice rather than
    /// an accident.
    /// </remarks>
    public bool TryConsumeAllowance(int stackId, IWiredPolicy policy, long nowMs)
    {
        if (policy.ExecutionLimit <= 0 || policy.ExecutionWindowMs <= 0)
        {
            _windows.Remove(stackId);

            return true;
        }

        if (!_windows.TryGetValue(stackId, out WiredExecutionWindow? window))
        {
            window = new WiredExecutionWindow();
            _windows[stackId] = window;
        }

        if (window.TryConsume(policy.ExecutionLimit, policy.ExecutionWindowMs, nowMs))
        {
            return true;
        }

        _diagnostics.ChainStopped(WiredStopReason.EXECUTION_LIMIT);

        return false;
    }

    /// <summary>Which of the pile's effects run this time.</summary>
    public List<IWiredAction> ChooseActions(
        int stackId,
        List<IWiredAction> actions,
        IWiredPolicy policy
    )
    {
        if (actions.Count == 0)
        {
            return [];
        }

        return policy.EffectMode switch
        {
            WiredEffectModeType.FirstOnly => [actions[0]],
            WiredEffectModeType.Random => ChooseRandomActions(stackId, actions, policy),
            WiredEffectModeType.Unseen => ChooseUnseenAction(stackId, actions),
            _ => [.. actions],
        };
    }

    /// <summary>
    /// One effect the pile has not run yet, in the pile's own order. When every effect has been seen
    /// the cycle starts over, so the pile keeps firing rather than falling silent once it has been
    /// through them all.
    /// </summary>
    private List<IWiredAction> ChooseUnseenAction(int stackId, List<IWiredAction> actions)
    {
        if (!_unseenCycles.TryGetValue(stackId, out WiredUnseenCycle? cycle))
        {
            cycle = new WiredUnseenCycle();
            _unseenCycles[stackId] = cycle;
        }

        int index = cycle.Next([.. actions.Select(ObjectIdOf)]);

        return index < 0 ? [] : [actions[index]];
    }

    /// <summary>
    /// The random add-on's draw: N effects, avoiding what the pile ran in its last M firings.
    /// </summary>
    private List<IWiredAction> ChooseRandomActions(
        int stackId,
        List<IWiredAction> actions,
        IWiredPolicy policy
    )
    {
        List<int> ids = [.. actions.Select(ObjectIdOf)];

        HashSet<int> recent = [];

        if (
            policy.EffectAvoidRecentExecutions > 0
            && _recentRandomPicks.TryGetValue(stackId, out Queue<HashSet<int>>? history)
        )
        {
            foreach (HashSet<int> firing in history)
            {
                recent.UnionWith(firing);
            }
        }

        List<int> picked = WiredRandomEffectPicker.Pick(
            ids,
            Math.Max(1, policy.EffectPickCount),
            recent,
            _random
        );

        RememberRandomPick(stackId, [.. picked.Select(index => ids[index])], policy);

        return [.. picked.Select(index => actions[index])];
    }

    private void RememberRandomPick(int stackId, HashSet<int> picked, IWiredPolicy policy)
    {
        if (policy.EffectAvoidRecentExecutions <= 0)
        {
            _recentRandomPicks.Remove(stackId);

            return;
        }

        if (!_recentRandomPicks.TryGetValue(stackId, out Queue<HashSet<int>>? history))
        {
            history = new Queue<HashSet<int>>();
            _recentRandomPicks[stackId] = history;
        }

        history.Enqueue(picked);

        while (history.Count > policy.EffectAvoidRecentExecutions)
        {
            history.Dequeue();
        }
    }

    private static int ObjectIdOf(IWiredAction action) =>
        (action as FurnitureWiredLogic)?.ObjectId.Value ?? 0;
}
