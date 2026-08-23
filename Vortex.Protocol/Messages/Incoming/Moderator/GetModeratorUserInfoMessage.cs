using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Moderator;

public record GetModeratorUserInfoMessage : IMessageEvent
{
    public required int UserId { get; init; }
}
