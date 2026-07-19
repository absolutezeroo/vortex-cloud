using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Engine;

public record MoveWallItemMessage : IMessageEvent
{
    public required int ObjectId { get; init; }
    public required string WallPosition { get; init; }
}
