using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Avatar;

public record ChangeMottoMessage : IMessageEvent
{
    public required string Text { get; init; }
}
