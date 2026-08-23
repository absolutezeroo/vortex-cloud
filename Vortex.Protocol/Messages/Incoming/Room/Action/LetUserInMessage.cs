using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Action;

public record LetUserInMessage : IMessageEvent
{
    public required string Username { get; init; }
    public required bool CanEnter { get; init; }
}
