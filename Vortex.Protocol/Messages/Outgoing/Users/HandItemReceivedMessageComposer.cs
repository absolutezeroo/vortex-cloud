using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record HandItemReceivedMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
