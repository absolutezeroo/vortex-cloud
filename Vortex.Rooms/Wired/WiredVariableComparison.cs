using Vortex.Primitives.Rooms.Enums.Wired;

namespace Vortex.Rooms.Wired;

/// <summary>
/// The six-way numeric comparison the variable boxes offer. The client sends the operator as the
/// radio button's own id (<c>&gt;</c> 2, <c>≥</c> 5, <c>=</c> 1, <c>≤</c> 3, <c>&lt;</c> 0,
/// <c>≠</c> 4), which is exactly <see cref="WiredComparisonType"/> — the order the buttons are
/// drawn in is not the order of the codes, so reading the selection as an index would map every
/// operator to the wrong one.
/// </summary>
public static class WiredVariableComparison
{
    public static bool Matches(long left, WiredComparisonType comparison, long right) =>
        comparison switch
        {
            WiredComparisonType.LessThan => left < right,
            WiredComparisonType.Equals => left == right,
            WiredComparisonType.GreaterThan => left > right,
            WiredComparisonType.LessThanOrEquals => left <= right,
            WiredComparisonType.NotEquals => left != right,
            WiredComparisonType.GreaterTHanOrEquals => left >= right,
            // An operator the client should never send: fail closed rather than pass everything.
            _ => false,
        };
}
