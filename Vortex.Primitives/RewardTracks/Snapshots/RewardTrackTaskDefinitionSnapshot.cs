using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.RewardTracks.Snapshots;

/// <summary>A task definition: what to do, how it is measured, and its stages.</summary>
[GenerateSerializer, Immutable]
public sealed record RewardTrackTaskDefinitionSnapshot
{
    /// <summary>Content id, unique within the track. Part of the client's localization stem.</summary>
    [Id(0)]
    public required string TaskId { get; init; }

    /// <summary>One of <see cref="RewardTrackActions"/>. Sent to the client as <c>actionType</c>.</summary>
    [Id(1)]
    public required string ActionCode { get; init; }

    /// <summary>
    /// Narrows the task to one target — a furniture class, a room id, a Habbicon id. Empty means
    /// any occurrence counts. Echoed to the client as <c>parameter</c>.
    /// </summary>
    [Id(2)]
    public required string Parameter { get; init; }

    [Id(3)]
    public required TaskProgressMode Mode { get; init; }

    /// <summary>The whole task is premium-only.</summary>
    [Id(4)]
    public required bool Premium { get; init; }

    [Id(5)]
    public required int SortOrder { get; init; }

    /// <summary>Stages in ascending <see cref="RewardTrackTaskLevelSnapshot.RequiredCount"/> order.</summary>
    [Id(6)]
    public required ImmutableArray<RewardTrackTaskLevelSnapshot> Levels { get; init; }

    /// <summary>
    /// Extra tests a signal must pass, all of them, on top of <see cref="Parameter"/>. Empty for
    /// almost every task — the default is "any occurrence of the action counts".
    /// </summary>
    /// <remarks>
    /// Additive to <see cref="Parameter"/> rather than a replacement for it: the parameter is on
    /// the wire and the client reads it, so removing it would be a protocol change to gain nothing.
    /// A task with neither behaves exactly as it did before conditions existed.
    /// </remarks>
    [Id(7)]
    public ImmutableArray<RewardTrackTaskConditionSnapshot> Conditions { get; init; } = [];

    /// <summary>The last stage's requirement — the point past which progress stops mattering.</summary>
    public int MaxRequiredCount => Levels.IsDefaultOrEmpty ? 0 : Levels[^1].RequiredCount;
}
