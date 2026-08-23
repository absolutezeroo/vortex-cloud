using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.GroupForums;

public record PostMessageMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required int ThreadId { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
}
