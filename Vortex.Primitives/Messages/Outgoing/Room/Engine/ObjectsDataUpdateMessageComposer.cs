using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Furniture.Snapshots.StuffData;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Outgoing.Room.Engine;

[GenerateSerializer, Immutable]
public sealed record ObjectsDataUpdateMessageComposer : IComposer
{
    [Id(0)]
    public required List<(RoomObjectId, StuffDataSnapshot)> StuffDatas { get; init; }
}
