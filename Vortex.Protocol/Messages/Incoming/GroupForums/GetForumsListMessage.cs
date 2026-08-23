using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.GroupForums;

public record GetForumsListMessage : IMessageEvent
{
    public required int ListCode { get; init; }
    public required int StartIndex { get; init; }
    public required int Amount { get; init; }
}
