using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Protocol.Messages.Incoming.Room.Furniture;

public record SetRandomStateMessage : IMessageEvent
{
    public required RoomObjectId ObjectId { get; init; }
    public required int Param { get; init; }
}
