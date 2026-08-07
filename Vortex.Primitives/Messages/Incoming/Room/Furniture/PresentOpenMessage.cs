using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Incoming.Room.Furniture;

/// <summary>
/// Unwrapping a present. The client sends only which one — what is inside is the server's to know,
/// and the widget shows the contents only after the answer comes back.
/// </summary>
public record PresentOpenMessage : IMessageEvent
{
    public required RoomObjectId ObjectId { get; init; }
}
