using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Users;

public record DeactivateGuildMessage : IMessageEvent
{
    public required int GroupId { get; init; }
}
