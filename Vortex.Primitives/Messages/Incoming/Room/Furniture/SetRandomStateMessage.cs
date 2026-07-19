using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Furniture;

public record SetRandomStateMessage : IMessageEvent
{
    public required int ObjectId { get; init; }
    public required int Param { get; init; }
}
