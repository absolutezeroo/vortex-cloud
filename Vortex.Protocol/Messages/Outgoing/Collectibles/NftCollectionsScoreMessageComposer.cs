using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Collectibles;

/// <summary>A player's collector standing: what they score now, their best, and the level it buys.</summary>
[GenerateSerializer, Immutable]
public sealed record NftCollectionsScoreMessageComposer : IComposer
{
    [Id(0)]
    public required int Score { get; init; }

    /// <summary>The best they have ever held, which does not fall when furniture is sold.</summary>
    [Id(1)]
    public required int HighestScore { get; init; }

    [Id(2)]
    public required int Level { get; init; }
}
