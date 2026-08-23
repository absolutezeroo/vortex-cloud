using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Inventory.Furni;

public record RequestRoomPropertySetMessage : IMessageEvent
{
    public required int RoomId { get; init; }
}
