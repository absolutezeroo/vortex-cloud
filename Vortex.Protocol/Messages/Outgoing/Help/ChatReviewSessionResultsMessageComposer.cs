using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Help;

/// <summary>
/// The verdict. Carries the guardian's own vote back alongside it, because the client shows them
/// what they picked next to what the group decided.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record ChatReviewSessionResultsMessageComposer : IComposer
{
    [Id(0)]
    public required int WinningVote { get; init; }

    [Id(1)]
    public required int OwnVote { get; init; }

    [Id(2)]
    public required ImmutableArray<int> FinalStatuses { get; init; }
}
