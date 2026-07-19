using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Inventory.Trading;

public record OpenTradingMessage : IMessageEvent
{
    /// <summary>Room-instance id (roomObjectId) of the avatar the requester clicked "Trade" on.
    /// The client sends the in-room object id here, not the web/account id, so the server resolves
    /// it back to a player through the room's avatar model.</summary>
    public required int OtherUserRoomObjectId { get; init; }
}
