using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Primitives.Messages.Incoming.Navigator;

public record UpdateHomeRoomMessage : IMessageEvent
{
    public RoomId RoomId { get; init; }
}
