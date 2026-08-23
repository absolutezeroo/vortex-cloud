using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Help;

/// <summary>Reporting a photo poster. The client sends no free-text message for this one — the
/// evidence is the image itself, identified by <see cref="PhotoId"/>.</summary>
public record CallForHelpFromPhotoMessage : IMessageEvent
{
    /// <summary>The photo's own id, read out of the furniture's JSON data.</summary>
    public required string PhotoId { get; init; }

    public required int RoomId { get; init; }

    /// <summary>Who took the photo — the player being reported.</summary>
    public required int PhotoAuthorId { get; init; }

    public required int TopicId { get; init; }

    /// <summary>The wall item displaying the photo, so staff can find it in the room.</summary>
    public required int FurniId { get; init; }
}
