using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Rooms.Wired;

/// <summary>
/// Whether a variable change is one the "variable changed" trigger was told to fire on.
/// </summary>
/// <remarks>
/// The form is three checkboxes — Created, Value changed, Deleted — and the middle one carries a
/// nested group of three: Increased, Decreased, Unchanged. The client sends them as
/// <c>[created, valueChanged, deleted, subMask]</c>, the last a bit per nested option in that order.
/// </remarks>
public static class WiredVariableChangeMatcher
{
    private const int IncreasedBit = 1 << 0;

    private const int DecreasedBit = 1 << 1;

    private const int UnchangedBit = 1 << 2;

    public static bool Matches(
        WiredVariableChangeKind kind,
        int previous,
        int current,
        bool onCreated,
        bool onValueChanged,
        bool onDeleted,
        int subMask
    ) =>
        kind switch
        {
            WiredVariableChangeKind.Created => onCreated,
            WiredVariableChangeKind.Deleted => onDeleted,
            WiredVariableChangeKind.ValueChanged => onValueChanged
                && MatchesDirection(previous, current, subMask),
            _ => false,
        };

    /// <summary>An empty sub-mask means the player ticked "Value changed" and left its three nested
    /// options alone, which asks for any write rather than for none of them.</summary>
    private static bool MatchesDirection(int previous, int current, int subMask)
    {
        if (subMask == 0)
        {
            return true;
        }

        int direction =
            current > previous ? IncreasedBit
            : current < previous ? DecreasedBit
            : UnchangedBit;

        return (subMask & direction) != 0;
    }
}
