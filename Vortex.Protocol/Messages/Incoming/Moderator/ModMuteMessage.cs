using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Moderator;

public record ModMuteMessage : IMessageEvent
{
    public required int UserId { get; init; }
    public required string Message { get; init; }
    public int TopicId { get; init; }

    /// <summary>-1 when this action isn't tied to a CFH ticket (the client omits the field).</summary>
    public int IssueId { get; init; } = -1;
}
