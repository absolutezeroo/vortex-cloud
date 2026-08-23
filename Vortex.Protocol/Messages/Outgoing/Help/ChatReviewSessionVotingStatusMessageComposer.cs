using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Help;

/// <summary>
/// How the vote is going: one entry per guardian taking part, so each client can show the others
/// filling in around them.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record ChatReviewSessionVotingStatusMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<int> Statuses { get; init; }
}
