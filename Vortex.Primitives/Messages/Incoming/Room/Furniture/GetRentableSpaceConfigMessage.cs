using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Furniture;

public record GetRentableSpaceConfigMessage : IMessageEvent
{
    public required int FurnitureId { get; init; }
}
