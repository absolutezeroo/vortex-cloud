using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Protocol.Messages.Incoming.Room.Engine;

public record ClickCharacterMessage : IMessageEvent
{
    /// <summary>Room object id of the clicked avatar — not the player's web id.</summary>
    public required RoomObjectId ObjectId { get; init; }
}
