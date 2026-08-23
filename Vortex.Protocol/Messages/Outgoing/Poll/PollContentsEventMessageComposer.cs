using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Polls.Snapshots;

namespace Vortex.Primitives.Messages.Outgoing.Poll;

/// <summary>
/// The whole survey, sent once when the player accepts the offer. The client drives the rest on
/// its own — it walks the questions locally and only talks back one PollAnswer at a time.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record PollContentsEventMessageComposer : IComposer
{
    [Id(0)]
    public required int PollId { get; init; }

    /// <summary>Shown above the first question.</summary>
    [Id(1)]
    public required string StartMessage { get; init; }

    /// <summary>Shown on the thank-you card.</summary>
    [Id(2)]
    public required string EndMessage { get; init; }

    /// <summary>Root questions only; each carries its own follow-ups.</summary>
    [Id(3)]
    public required ImmutableArray<PollQuestionSnapshot> Questions { get; init; }

    /// <summary>Enables the client's branching walk through follow-up questions.</summary>
    [Id(4)]
    public required bool NpsPoll { get; init; }
}
