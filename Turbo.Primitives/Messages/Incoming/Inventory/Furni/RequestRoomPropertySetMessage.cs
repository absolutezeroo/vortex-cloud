using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Inventory.Furni;

public record RequestRoomPropertySetMessage : IMessageEvent
{
    public required int RoomId { get; init; }
}
