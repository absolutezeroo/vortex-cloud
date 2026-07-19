using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Messages.Incoming.Room.Engine;

public record MoveObjectMessage : IMessageEvent
{
    public required int ObjectId { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required Rotation Rotation { get; init; }
}
