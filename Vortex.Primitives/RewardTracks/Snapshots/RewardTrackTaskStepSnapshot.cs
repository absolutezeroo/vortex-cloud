using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.RewardTracks.Snapshots;

/// <summary>
/// One action in a task's sequence, with the tests a signal must pass to satisfy it.
/// </summary>
/// <remarks>
/// Server-side only. The client is told one <c>actionType</c> and one <c>parameter</c> per task, so
/// a multi-step task shows the icon of the action it starts with and its own text has to spell out
/// the rest. A sequence narrows what counts; it is never what the player reads.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record RewardTrackTaskStepSnapshot
{
    /// <summary>Zero-based position in the sequence.</summary>
    [Id(0)]
    public required int StepIndex { get; init; }

    /// <summary>One of <see cref="RewardTrackActions"/>.</summary>
    [Id(1)]
    public required string ActionCode { get; init; }

    /// <summary>All of these must hold. Empty means any occurrence of the action satisfies the step.</summary>
    [Id(2)]
    public ImmutableArray<RewardTrackStepFilterSnapshot> Filters { get; init; } = [];
}

/// <summary>
/// One test on a signal's facts.
/// </summary>
/// <remarks>
/// <see cref="Value"/> is either a literal, or a back-reference of the form <c>$N</c> naming an
/// earlier step. A back-reference resolves to the value that step recorded <em>for this same
/// fact key</em> — which is what "walk on the furniture you just placed" means, and why the
/// capture needs no separate row: every step remembers all of its facts.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record RewardTrackStepFilterSnapshot
{
    /// <summary>Which fact to look at, from <see cref="RewardTrackFacts"/>.</summary>
    [Id(0)]
    public required string FactKey { get; init; }

    [Id(1)]
    public required StepFilterOperator Operator { get; init; }

    /// <summary>A literal, a comma-separated list for <see cref="StepFilterOperator.OneOf"/>, or <c>$N</c>.</summary>
    [Id(2)]
    public required string Value { get; init; }

    /// <summary>
    /// The step this filter points back at, or <c>-1</c> when the value is a literal. Parsed once
    /// by the catalog so the hot path never re-reads the string.
    /// </summary>
    [Id(3)]
    public int ReferencedStep { get; init; } = -1;
}
