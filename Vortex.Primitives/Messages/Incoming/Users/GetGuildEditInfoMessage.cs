using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Users;

public record GetGuildEditInfoMessage : IMessageEvent
{
    public required int GroupId { get; init; }
}
