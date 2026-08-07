using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Incoming.Room.Furniture;

/// <summary>
/// The moodlight's on/off switch. There is no target state on the wire: the client asks for a
/// toggle and the server decides what that means.
/// </summary>
public record RoomDimmerChangeStateMessage : IMessageEvent
{
    public required RoomObjectId ObjectId { get; init; }
}
