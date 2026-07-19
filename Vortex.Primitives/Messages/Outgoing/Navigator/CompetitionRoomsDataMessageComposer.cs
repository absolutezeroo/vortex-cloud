using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Snapshots.Navigator;

namespace Vortex.Primitives.Messages.Outgoing.Navigator;

[GenerateSerializer, Immutable]
public sealed record CompetitionRoomsDataMessageComposer : IComposer
{
    [Id(0)]
    public required CompetitionRoomDataSnapshot RoomData { get; init; }
}
