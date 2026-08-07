using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Incoming.Room.Furniture;

/// <summary>
/// Opening the moodlight dialog. The client sends nothing but the dimmer it is asking about — it
/// holds no presets of its own and cannot draw the window until the server answers.
/// </summary>
public record RoomDimmerGetPresetsMessage : IMessageEvent
{
    public required RoomObjectId ObjectId { get; init; }
}
