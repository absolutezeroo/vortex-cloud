using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Primitives.Rooms.Events;

/// <summary>
/// A wired variable's value was created, written or removed. Raised by the variable box that owns
/// the value, whoever asked for the write — a wired action, the wired menu — so the "variable
/// changed" trigger fires on all of them rather than only on the paths one action happens to take.
/// </summary>
public sealed record WiredVariableChangedEvent : RoomEvent
{
    /// <summary>Which variable, and which target it was stored against.</summary>
    public required WiredVariableKey Key { get; init; }

    public required WiredVariableChangeKind Kind { get; init; }

    /// <summary>The value before the write; 0 when the key did not exist.</summary>
    public int Previous { get; init; }

    /// <summary>The value after the write; 0 for a deletion. The trigger's sub-options ask about the
    /// direction between the two.</summary>
    public int Current { get; init; }
}
