using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Nft;

[GenerateSerializer, Immutable]
public sealed record UserNftWardrobeSelectionMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
