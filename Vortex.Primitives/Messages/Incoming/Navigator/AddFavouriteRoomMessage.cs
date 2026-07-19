using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms;

namespace Vortex.Primitives.Messages.Incoming.Navigator;

public record AddFavouriteRoomMessage : IMessageEvent
{
    public RoomId RoomId { get; init; }
}
