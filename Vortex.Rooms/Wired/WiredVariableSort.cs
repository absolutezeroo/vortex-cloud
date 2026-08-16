namespace Vortex.Rooms.Wired;

/// <summary>
/// How the variable filter add-ons rank what they keep, as the client's dropdown ids.
/// </summary>
/// <remarks>
/// The dropdown only offers the pairs the picked variable can answer — the value pair for a
/// variable that holds one, the creation pair for one that remembers when it was made, the update
/// pair for one that remembers when it last moved.
/// </remarks>
public enum WiredVariableSort
{
    HighestValue = 0,
    LowestValue = 1,
    OldestCreation = 2,
    LatestCreation = 3,
    OldestUpdate = 4,
    LatestUpdate = 5,
}

public static class WiredVariableSortExtensions
{
    /// <summary>Whether the mode ranks by the value rather than by one of the two moments.</summary>
    public static bool RanksByValue(this WiredVariableSort sort) =>
        sort is WiredVariableSort.HighestValue or WiredVariableSort.LowestValue;

    /// <summary>Whether the mode ranks by when the value was first written.</summary>
    public static bool RanksByCreation(this WiredVariableSort sort) =>
        sort is WiredVariableSort.OldestCreation or WiredVariableSort.LatestCreation;

    /// <summary>Whether the biggest number wins — the latest moment being the biggest one.</summary>
    public static bool WantsDescending(this WiredVariableSort sort) =>
        sort
            is WiredVariableSort.HighestValue
                or WiredVariableSort.LatestCreation
                or WiredVariableSort.LatestUpdate;
}
