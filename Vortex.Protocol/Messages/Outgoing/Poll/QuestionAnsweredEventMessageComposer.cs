using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Polls.Snapshots;

namespace Vortex.Protocol.Messages.Outgoing.Poll;

/// <summary>
/// Broadcast when someone answers the live question: it names the answering avatar so the client
/// can make them smile or frown, and carries the refreshed tally for the whole room.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record QuestionAnsweredEventMessageComposer : IComposer
{
    /// <summary>The player who just answered.</summary>
    [Id(0)]
    public required int UserId { get; init; }

    /// <summary>The answer they picked.</summary>
    [Id(1)]
    public required string Value { get; init; }

    /// <summary>Running tally over every answer given so far.</summary>
    [Id(2)]
    public required ImmutableArray<PollAnswerCountSnapshot> AnswerCounts { get; init; }
}
