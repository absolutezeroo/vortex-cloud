using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.GroupForums;

public record ModerateThreadMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required int ThreadId { get; init; }
    public required int Action { get; init; }
}
