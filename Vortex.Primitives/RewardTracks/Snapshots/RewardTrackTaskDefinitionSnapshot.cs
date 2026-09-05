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
    /// The actions the player has to perform, in order, for this task to count once. Never empty:
    /// the catalog materialises a single step from <see cref="ActionCode"/> and
    /// <see cref="Parameter"/> for a plain task, so the engine only ever walks steps and the simple
    /// case is not a second code path.
    /// </summary>
    /// <remarks>
    /// Step 0 always mirrors <see cref="ActionCode"/>. That field is what the client draws the
    /// task's icon from and there is exactly one per task on the wire, so a sequence shows the
    /// picture of the action it starts with and the task's own text has to spell out the rest.
    /// </remarks>
    [Id(7)]
    public ImmutableArray<RewardTrackTaskStepSnapshot> Steps { get; init; } = [];

    /// <summary>Whether this task is a sequence rather than a single action.</summary>
    public bool IsSequence => Steps.Length > 1;

    /// <summary>The last stage's requirement — the point past which progress stops mattering.</summary>
    public int MaxRequiredCount => Levels.IsDefaultOrEmpty ? 0 : Levels[^1].RequiredCount;
}
