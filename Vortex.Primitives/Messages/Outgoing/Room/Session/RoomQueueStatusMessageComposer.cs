using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Session;

[GenerateSerializer, Immutable]
public sealed record RoomQueueStatusMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
