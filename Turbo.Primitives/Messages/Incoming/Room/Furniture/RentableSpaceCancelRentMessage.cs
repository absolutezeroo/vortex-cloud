using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Room.Furniture;

public record RentableSpaceCancelRentMessage : IMessageEvent
{
    public required int FurnitureId { get; init; }
}
