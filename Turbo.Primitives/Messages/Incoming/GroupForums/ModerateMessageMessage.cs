using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.GroupForums;

public record ModerateMessageMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required int ThreadId { get; init; }
    public required int MessageId { get; init; }
    public required int Action { get; init; }
}
