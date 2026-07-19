using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Outgoing.Room.Engine;

[GenerateSerializer, Immutable]
public sealed record ItemsStateUpdateMessageComposer : IComposer
{
    [Id(0)]
    public required List<(RoomObjectId, string)> ObjectStates { get; init; }
}
