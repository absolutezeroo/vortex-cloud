using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Protocol.Messages.Incoming.Navigator;

public record RemoveOwnRoomRightsRoomMessage : IMessageEvent
{
    public RoomId RoomId { get; init; }
}
