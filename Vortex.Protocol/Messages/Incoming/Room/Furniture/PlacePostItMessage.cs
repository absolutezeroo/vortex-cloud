using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Primitives.Messages.Incoming.Room.Furniture;

/// <summary>Sticking a note on the wall. Placement only — the note's colour and text arrive later,
/// through <see cref="AddSpamWallPostItMessage"/>, once the editor closes.</summary>
public record PlacePostItMessage : IMessageEvent
{
    public required RoomObjectId ObjectId { get; init; }

    /// <summary>Raw wall coordinate string (":w=x,y l=x,y r"), left unparsed here.</summary>
    public required string Location { get; init; }
}
