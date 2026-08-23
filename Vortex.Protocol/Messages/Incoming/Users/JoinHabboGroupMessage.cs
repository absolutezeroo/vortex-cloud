using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Users;

public record JoinHabboGroupMessage : IMessageEvent
{
    public required int GroupId { get; init; }
}
