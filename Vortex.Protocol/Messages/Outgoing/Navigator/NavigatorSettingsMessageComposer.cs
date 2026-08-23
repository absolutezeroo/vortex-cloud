using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Protocol.Messages.Outgoing.Navigator;

[GenerateSerializer, Immutable]
public sealed record NavigatorSettingsMessageComposer : IComposer
{
    [Id(0)]
    public RoomId HomeRoomId { get; init; }

    [Id(1)]
    public RoomId RoomIdToEnter { get; init; }
}
