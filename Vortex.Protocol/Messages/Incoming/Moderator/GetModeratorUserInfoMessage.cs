using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Moderator;

public record GetModeratorUserInfoMessage : IMessageEvent
{
    public required int UserId { get; init; }
}
