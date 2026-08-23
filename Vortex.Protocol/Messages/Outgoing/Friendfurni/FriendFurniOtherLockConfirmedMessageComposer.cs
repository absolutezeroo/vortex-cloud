using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Friendfurni;

[GenerateSerializer, Immutable]
public sealed record FriendFurniOtherLockConfirmedMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
