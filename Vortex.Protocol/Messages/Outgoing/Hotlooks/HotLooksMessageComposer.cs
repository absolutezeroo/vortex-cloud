using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Hotlooks;

[GenerateSerializer, Immutable]
public sealed record HotLooksMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
