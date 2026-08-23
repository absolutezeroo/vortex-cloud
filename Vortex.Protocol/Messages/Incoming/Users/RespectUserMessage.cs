using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Users;

/// <summary>A player gives a respect point to the player identified by <see cref="UserId"/>.</summary>
public record RespectUserMessage : IMessageEvent
{
    public required int UserId { get; init; }
}
