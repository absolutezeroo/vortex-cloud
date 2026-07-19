using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Collectibles;

[GenerateSerializer, Immutable]
public sealed record NftTransferFeeMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
