using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Protocol.Messages.Incoming.Room.Engine;

public record MoveWallItemMessage : IMessageEvent
{
    public required RoomObjectId ObjectId { get; init; }
    public required string WallPosition { get; init; }
}
