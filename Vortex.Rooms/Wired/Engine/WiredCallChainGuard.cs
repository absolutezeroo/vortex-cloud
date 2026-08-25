using System;
using System.Collections.Generic;
using Vortex.Primitives.Observability;

namespace Vortex.Rooms.Wired.Engine;

/// <summary>
/// Stops an "execute stacks" chain from entering a pile it is already inside, and bounds how far it
/// may go.
/// </summary>
/// <remarks>
/// <para>
/// Two separate protections that are easy to confuse. The tile set makes a cycle <em>impossible</em>:
/// a pile that calls itself, or two piles that call each other, would otherwise recurse until the
/// room fell over. The depth limit bounds the cost of a wide chain that is perfectly legitimate and
/// simply large.
/// </para>
/// <para>
/// Both are counted rather than silently applied, because "why did my wired stop" is otherwise
/// unanswerable from outside the room.
/// </para>
/// </remarks>
internal sealed class WiredCallChainGuard(IWiredDiagnostics diagnostics, Func<int> maxDepth)
{
    private readonly IWiredDiagnostics _diagnostics = diagnostics;
    private readonly Func<int> _maxDepth = maxDepth;

    // Tiles whose pile is somewhere in the chain currently running.
    private readonly HashSet<int> _tiles = [];

    // The ids of the chain steps currently open, outermost first. Room-scoped and monotonic rather
    // than a guid: the wired log is read one room at a time and indexed by room, so a small number
    // is enough to tell two interleaved chains apart and costs nothing to write on every line.
    private readonly List<int> _executions = [];
    private int _nextExecutionId;

    /// <summary>How many piles deep the chain currently is.</summary>
    public int Depth => _tiles.Count;

    /// <summary>
    /// The chain step currently running, or 0 when nothing is. Stamped on every room-log line so the
    /// log stops being a flat list and becomes a chronology somebody can follow: which line belongs
    /// to which chain, and which chain called it.
    /// </summary>
    public int CurrentExecutionId => _executions.Count > 0 ? _executions[^1] : 0;

    /// <summary>The step that called the current one, or 0 when the current one started the chain.</summary>
    public int ParentExecutionId => _executions.Count > 1 ? _executions[^2] : 0;

    /// <summary>
    /// Whether the chain has room to go one level deeper. Counts a <c>depth</c> stop when it does
    /// not — the counter OQ-1 needs to settle whether the limit should be 8 or 20.
    /// </summary>
    public bool HasRoomToDescend()
    {
        if (_tiles.Count < _maxDepth())
        {
            return true;
        }

        _diagnostics.ChainStopped(WiredStopReason.DEPTH);

        return false;
    }

    /// <summary>
    /// Takes a hold on a tile for the duration of the returned scope, or reports that the chain is
    /// already inside it.
    /// </summary>
    /// <remarks>
    /// A refused entry counts a <c>cycle</c> stop. A negative tile index is not a tile — it is the
    /// caller saying it has none, which happens when a chain starts from something that is not a
    /// pile — and holding it would be holding nothing.
    /// </remarks>
    public Hold Enter(int tileIdx)
    {
        if (tileIdx < 0)
        {
            return new Hold(this, tileIdx, held: false, isCycle: false);
        }

        if (_tiles.Add(tileIdx))
        {
            _executions.Add(++_nextExecutionId);

            return new Hold(this, tileIdx, held: true, isCycle: false);
        }

        _diagnostics.ChainStopped(WiredStopReason.CYCLE);

        return new Hold(this, tileIdx, held: false, isCycle: true);
    }

    private void Release(int tileIdx)
    {
        _tiles.Remove(tileIdx);

        if (_executions.Count > 0)
        {
            _executions.RemoveAt(_executions.Count - 1);
        }
    }

    /// <summary>
    /// One tile held for the length of a chain step. Disposing releases it — including when the step
    /// throws, which is the reason this is a scope rather than a pair of calls.
    /// </summary>
    internal readonly struct Hold : IDisposable
    {
        private readonly WiredCallChainGuard _guard;
        private readonly int _tileIdx;
        private readonly bool _held;

        internal Hold(WiredCallChainGuard guard, int tileIdx, bool held, bool isCycle)
        {
            _guard = guard;
            _tileIdx = tileIdx;
            _held = held;
            IsCycle = isCycle;
        }

        /// <summary>The chain is already inside this pile, so the step must be skipped.</summary>
        public bool IsCycle { get; }

        public void Dispose()
        {
            if (_held)
            {
                _guard.Release(_tileIdx);
            }
        }
    }
}
