using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Protocol.Messages.Incoming.Room.Furniture;

/// <summary>The note's contents when the editor closes.</summary>
public record AddSpamWallPostItMessage : IMessageEvent
{
    public required RoomObjectId ObjectId { get; init; }

    /// <summary>Raw wall coordinate string, left unparsed here.</summary>
    public required string Location { get; init; }

    /// <summary>Paper colour as the client's hex string, e.g. "FFFF33".</summary>
    public required string ColorHex { get; init; }

    public required string Text { get; init; }
}
