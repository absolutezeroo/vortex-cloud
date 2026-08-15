namespace Vortex.Primitives.Rooms.Enums.Wired;

/// <summary>
/// What happened to a wired variable, matching the three checkboxes the "variable changed" trigger
/// offers ("Created", "Value changed", "Deleted").
/// </summary>
public enum WiredVariableChangeKind
{
    /// <summary>The key did not exist and now holds a value.</summary>
    Created,

    /// <summary>The key already existed and was written again — including with the same value,
    /// which the trigger's sub-options can ask for specifically.</summary>
    ValueChanged,

    /// <summary>The key was removed.</summary>
    Deleted,
}
