using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.GroupForums;

public record ModerateThreadMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required int ThreadId { get; init; }
    public required int Action { get; init; }
}
