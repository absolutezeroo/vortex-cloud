using System.Collections.Generic;
using System.Linq;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Wired;

/// <summary>
/// Which half of a pile's actions runs: the ordinary ones when the conditions held, the negative
/// ones when they did not.
/// </summary>
/// <remarks>
/// Habbo's wired has an else. The furni say it in as many words — "WIRED Negative Effect: ...
/// Allows you to select wired stacks to execute when a trigger fires but its conditions are not
/// met" — and without this split a negative action runs on success instead, which is not a missing
/// feature but an inverted one.
/// </remarks>
public static class WiredActionBranch
{
    /// <summary>
    /// The actions to run for this firing. A pile with no negative action and failing conditions
    /// yields nothing, which is the old behaviour and the common case.
    /// </summary>
    public static List<IWiredAction> Select(
        IReadOnlyList<IWiredAction> actions,
        bool conditionsPassed
    ) => [.. actions.Where(action => action.IsNegative() != conditionsPassed)];
}
