using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Collectibles;

[GenerateSerializer, Immutable]
public sealed record CollectibleMintTokenOffersMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
