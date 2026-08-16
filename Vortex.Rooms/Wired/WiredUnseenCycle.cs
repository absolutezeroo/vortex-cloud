using System.Collections.Generic;

namespace Vortex.Rooms.Wired;

/// <summary>
/// One pile's round through its effects for the "unseen effect" add-on: each firing runs one effect
/// that has not run yet, and the round starts over once they all have.
/// </summary>
/// <remarks>
/// Starting over matters more than it looks: without it a pile goes permanently silent the moment
/// it has been through its effects once, which reads in-game as the wiring having broken.
/// </remarks>
public sealed class WiredUnseenCycle
{
    private readonly HashSet<int> _seen = [];

    /// <summary>
    /// The index of the next effect to run, in the pile's own order, or -1 when the pile has no
    /// effects. Marks it seen.
    /// </summary>
    public int Next(IReadOnlyList<int> effectIds)
    {
        if (effectIds.Count == 0)
        {
            return -1;
        }

        int index = -1;

        for (int i = 0; i < effectIds.Count; i++)
        {
            if (!_seen.Contains(effectIds[i]))
            {
                index = i;

                break;
            }
        }

        if (index < 0)
        {
            // Everything has been through; the effects a pile no longer has drop out with it.
            _seen.Clear();
            index = 0;
        }

        _seen.Add(effectIds[index]);

        return index;
    }
}
