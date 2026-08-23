using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Incoming.Room.Engine;

public record MoveObjectMessage : IMessageEvent
{
    public required RoomObjectId ObjectId { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required Rotation Rotation { get; init; }
}
