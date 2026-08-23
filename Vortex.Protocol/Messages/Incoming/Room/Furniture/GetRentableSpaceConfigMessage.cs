using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Furniture;

public record GetRentableSpaceConfigMessage : IMessageEvent
{
    public required int FurnitureId { get; init; }
}
