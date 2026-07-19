using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Layout;

[GenerateSerializer, Immutable]
public sealed record RoomOccupiedTilesMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
