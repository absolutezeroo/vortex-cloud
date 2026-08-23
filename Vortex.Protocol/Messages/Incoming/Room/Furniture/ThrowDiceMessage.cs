using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Furniture;

public record ThrowDiceMessage : IMessageEvent
{
    public required int ObjectId { get; init; }
}
