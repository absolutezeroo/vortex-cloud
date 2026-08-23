using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Help;

/// <summary>Reporting a selfie. Unlike the photo variant this one carries a written message and a
/// share URL instead of a photo id, and no topic — the client offers a single selfie report reason.</summary>
public record CallForHelpFromSelfieMessage : IMessageEvent
{
    /// <summary>The image's public share URL, or "url not available" when the client has none.</summary>
    public required string Url { get; init; }

    public required int RoomId { get; init; }

    public required int PhotoAuthorId { get; init; }

    public required string Message { get; init; }

    public required int FurniId { get; init; }
}
