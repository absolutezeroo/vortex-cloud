using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Users;

public record KickMemberMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required int UserId { get; init; }
    public required bool Block { get; init; }
}
