using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Action;

public record LetUserInMessage : IMessageEvent
{
    public required string Username { get; init; }
    public required bool CanEnter { get; init; }
}
