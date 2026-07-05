using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Moderator;

public record DefaultSanctionMessage : IMessageEvent
{
    public required int UserId { get; init; }

    public required int TopicId { get; init; }

    public required string Message { get; init; }

    public int IssueId { get; init; } = -1;
}
