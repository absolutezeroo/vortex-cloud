using Orleans;

namespace Vortex.Primitives.RewardTracks.Snapshots;

/// <summary>
/// One extra test a signal must pass before it advances a task. A task's conditions are ANDed:
/// all of them, or the signal is ignored.
/// </summary>
/// <remarks>
/// Server-side only. The client is told a task's <c>actionType</c> and <c>parameter</c> and nothing
/// else, so a condition can narrow what counts but can never be the thing the player reads — the
/// task's own text has to say what it wants.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record RewardTrackTaskConditionSnapshot
{
    [Id(0)]
    public required TaskConditionField Field { get; init; }

    [Id(1)]
    public required TaskConditionOperator Operator { get; init; }

    /// <summary>
    /// The value to compare against. For <see cref="TaskConditionOperator.OneOf"/> a
    /// comma-separated list; for a numeric operator, a number. Kept as a string because the field
    /// it is compared to is one — the signal's target has no type of its own.
    /// </summary>
    [Id(2)]
    public required string Value { get; init; }
}
