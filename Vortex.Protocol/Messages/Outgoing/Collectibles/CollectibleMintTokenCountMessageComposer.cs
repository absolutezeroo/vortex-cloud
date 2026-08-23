using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Collectibles;

/// <summary>How many mint tokens the player holds. One int, and zero is a real answer.</summary>
[GenerateSerializer, Immutable]
public sealed record CollectibleMintTokenCountMessageComposer : IComposer
{
    [Id(0)]
    public required int Count { get; init; }
}
