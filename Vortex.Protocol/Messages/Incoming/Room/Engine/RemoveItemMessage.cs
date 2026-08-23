using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Protocol.Messages.Incoming.Room.Engine;

/// <summary>
/// Throwing a wall item away. Not a pickup — the client's sticky and photo widgets send this from
/// their bin button, and what it names is destroyed rather than returned to anyone's inventory.
/// </summary>
public record RemoveItemMessage : IMessageEvent
{
    public required RoomObjectId ObjectId { get; init; }
}
