using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Handshake;

[GenerateSerializer, Immutable]
public sealed record DisconnectReasonEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
