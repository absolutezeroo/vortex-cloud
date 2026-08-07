using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Incoming.Room.Furniture;

/// <summary>
/// Asking to pass through a one-way gate. Sent on a double-click and nothing else — the client does
/// not walk the avatar there first, so the player is already standing where they will be judged
/// from.
/// </summary>
public record EnterOneWayDoorMessage : IMessageEvent
{
    public required RoomObjectId ObjectId { get; init; }
}
