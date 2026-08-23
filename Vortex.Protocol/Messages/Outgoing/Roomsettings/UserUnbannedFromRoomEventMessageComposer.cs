using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Roomsettings;

[GenerateSerializer, Immutable]
public sealed record UserUnbannedFromRoomEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
