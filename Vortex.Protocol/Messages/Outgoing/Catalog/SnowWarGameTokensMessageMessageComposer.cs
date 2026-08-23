using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record SnowWarGameTokensMessageMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
