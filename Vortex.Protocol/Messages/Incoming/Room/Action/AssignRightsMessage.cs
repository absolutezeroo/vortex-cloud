using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Action;

public record AssignRightsMessage : IMessageEvent
{
    public required int TargetUserId { get; init; }
}
