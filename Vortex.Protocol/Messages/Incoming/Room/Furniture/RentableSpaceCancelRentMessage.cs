using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Furniture;

public record RentableSpaceCancelRentMessage : IMessageEvent
{
    public required int FurnitureId { get; init; }
}
