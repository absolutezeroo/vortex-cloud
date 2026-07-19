using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Hotlooks;

[GenerateSerializer, Immutable]
public sealed record HotLooksMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
