using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Furniture;

public record RentableSpaceStatusMessage : IMessageEvent
{
    public required int FurnitureId { get; init; }
}
