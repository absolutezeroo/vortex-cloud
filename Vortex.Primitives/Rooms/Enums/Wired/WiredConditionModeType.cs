namespace Vortex.Primitives.Rooms.Enums.Wired;

/// <summary>
/// How many of a pile's conditions must pass for its trigger to fire. Mirrors the client's
/// <c>eval_mode</c> radio group on the condition-evaluation addon, labelled "Conditions that need to
/// match:". The four set modes are sent as their own id; the three counting modes arrive as
/// <c>mode = -1</c> plus a compare type, and are folded into this enum by the addon.
/// <para>
/// Values 0-3 match the client's wire ids, so an incoming mode casts straight across. Nothing
/// persists this enum numerically — it only ever lives on a runtime policy — so extending it is safe.
/// </para>
/// </summary>
public enum WiredConditionModeType
{
    /// <summary>"All" — every condition must pass. The default when no evaluation addon is stacked.</summary>
    All = 0,

    /// <summary>"At least one" — a single passing condition is enough.</summary>
    AtLeastOne = 1,

    /// <summary>"Not all" — fires as long as at least one condition fails.</summary>
    NotAll = 2,

    /// <summary>"None" — fires only when no condition passes.</summary>
    None = 3,

    /// <summary>"Less than N" — fewer than the configured number of conditions pass.</summary>
    CountLessThan = 4,

    /// <summary>"Exactly N" — exactly the configured number of conditions pass.</summary>
    CountExactly = 5,

    /// <summary>"More than N" — more than the configured number of conditions pass.</summary>
    CountMoreThan = 6,
}
