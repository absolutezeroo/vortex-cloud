using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Engine;

public record UseFurnitureMessage : IMessageEvent
{
    public required int ObjectId { get; init; }
    public required int Param { get; init; }
}
