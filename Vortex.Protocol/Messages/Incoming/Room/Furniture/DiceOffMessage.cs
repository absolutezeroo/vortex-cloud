using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Furniture;

public record DiceOffMessage : IMessageEvent
{
    public required int ObjectId { get; init; }
}
