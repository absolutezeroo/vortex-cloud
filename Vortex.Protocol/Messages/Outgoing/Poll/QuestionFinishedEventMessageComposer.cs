using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Polls.Snapshots;

namespace Vortex.Primitives.Messages.Outgoing.Poll;

/// <summary>Closes the live question and leaves the final tally on screen.</summary>
[GenerateSerializer, Immutable]
public sealed record QuestionFinishedEventMessageComposer : IComposer
{
    [Id(0)]
    public required int QuestionId { get; init; }

    /// <summary>Final counts per answer.</summary>
    [Id(1)]
    public required ImmutableArray<PollAnswerCountSnapshot> AnswerCounts { get; init; }
}
