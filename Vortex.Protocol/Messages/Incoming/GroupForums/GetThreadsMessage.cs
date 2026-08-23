using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.GroupForums;

public record GetThreadsMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required int StartIndex { get; init; }
    public required int Amount { get; init; }
}
