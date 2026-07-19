using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Engine;

public record ClickCharacterMessage : IMessageEvent
{
    public required int UserId { get; init; }
}
