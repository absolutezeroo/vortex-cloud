using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>
/// Cashing in a credit furni. The client sends only which one — what it is worth is read from the
/// definition, never from the packet.
/// </summary>
public record CreditFurniRedeemMessage : IMessageEvent
{
    public required RoomObjectId ObjectId { get; init; }
}
