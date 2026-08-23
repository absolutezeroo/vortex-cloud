using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Help;

/// <summary>
/// The excerpt to judge, sent to a guardian once they have taken the review.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record ChatReviewSessionStartedMessageComposer : IComposer
{
    [Id(0)]
    public required int VotingTimeoutSeconds { get; init; }

    [Id(1)]
    public required string ChatRecord { get; init; }
}
