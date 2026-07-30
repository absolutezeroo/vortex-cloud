using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Incoming.Room.Furniture;

/// <summary>
/// Sent when the player confirms the mystery trophy dialog: the trophy furniture to open and the
/// inscription typed into it.
/// </summary>
public record OpenMysteryTrophyMessage : IMessageEvent
{
    public required RoomObjectId ObjectId { get; init; }
    public required string Inscription { get; init; }
}
