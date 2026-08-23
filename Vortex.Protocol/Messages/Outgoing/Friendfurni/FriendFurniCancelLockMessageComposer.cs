using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Friendfurni;

[GenerateSerializer, Immutable]
public sealed record FriendFurniCancelLockMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
