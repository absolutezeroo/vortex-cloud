using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Users;

public record GetMemberGuildItemCountMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required int UserId { get; init; }
}
