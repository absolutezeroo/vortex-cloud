using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Action;

public record AssignRightsMessage : IMessageEvent
{
    public required int TargetUserId { get; init; }
}
