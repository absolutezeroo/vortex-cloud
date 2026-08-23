using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Collectibles;

/// <summary>What a wallet transfer would cost. One int.</summary>
[GenerateSerializer, Immutable]
public sealed record NftTransferFeeMessageComposer : IComposer
{
    [Id(0)]
    public required int Fee { get; init; }
}
