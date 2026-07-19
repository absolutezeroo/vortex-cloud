using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Users;

public record DeactivateGuildMessage : IMessageEvent
{
    public required int GroupId { get; init; }
}
