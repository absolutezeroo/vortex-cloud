using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Collectibles;

[GenerateSerializer, Immutable]
public sealed record NftBonusItemClaimResultMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
