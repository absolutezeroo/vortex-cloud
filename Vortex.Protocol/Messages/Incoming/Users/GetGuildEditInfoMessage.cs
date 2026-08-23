using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Users;

public record GetGuildEditInfoMessage : IMessageEvent
{
    public required int GroupId { get; init; }
}
