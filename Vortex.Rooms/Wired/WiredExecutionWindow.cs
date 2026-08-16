using System.Collections.Generic;

namespace Vortex.Rooms.Wired;

/// <summary>
/// One pile's "at most N firings per window", for the execution-limit add-on.
/// </summary>
/// <remarks>
/// The window is rolling, not a bucket that expires: firings drop out as they age, so a pile
/// limited to three per five seconds can fire again five seconds after its third rather than
/// waiting for a period boundary.
/// <para>
/// Ephemeral by design, like the rest of the wired runtime state — a room that unloads starts its
/// windows again.
/// </para>
/// </remarks>
public sealed class WiredExecutionWindow
{
    private readonly Queue<long> _firings = new();

    /// <summary>
    /// Whether the pile may fire at <paramref name="nowMs"/>, recording the firing when it may. A
    /// limit or window of zero is no limit at all, which is what a pile without the add-on has.
    /// </summary>
    public bool TryConsume(int limit, int windowMs, long nowMs)
    {
        if (limit <= 0 || windowMs <= 0)
        {
            _firings.Clear();

            return true;
        }

        while (_firings.Count > 0 && nowMs - _firings.Peek() >= windowMs)
        {
            _firings.Dequeue();
        }

        if (_firings.Count >= limit)
        {
            return false;
        }

        _firings.Enqueue(nowMs);

        return true;
    }
}
