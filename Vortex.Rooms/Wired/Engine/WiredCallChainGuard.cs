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

    /// <summary>How many piles deep the chain currently is.</summary>
    public int Depth => _tiles.Count;

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
            return new Hold(this, tileIdx, held: true, isCycle: false);
        }

        _diagnostics.ChainStopped(WiredStopReason.CYCLE);

        return new Hold(this, tileIdx, held: false, isCycle: true);
    }

    private void Release(int tileIdx) => _tiles.Remove(tileIdx);

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
