using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Avatar;

public record ChangeMottoMessage : IMessageEvent
{
    public required string Text { get; init; }
}
