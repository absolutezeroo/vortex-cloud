using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Collectibles;

[GenerateSerializer, Immutable]
public sealed record CollectableMintableItemTypesMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
