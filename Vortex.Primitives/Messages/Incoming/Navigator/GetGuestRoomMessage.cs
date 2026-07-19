using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Primitives.Messages.Incoming.Navigator;

public record GetGuestRoomMessage : IMessageEvent
{
    public RoomId RoomId { get; init; }
    public bool EnterRoom { get; init; }
    public bool RoomForward { get; init; }
}
