using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Users;

/// <summary>A player gives a respect point to the player identified by <see cref="UserId"/>.</summary>
public record RespectUserMessage : IMessageEvent
{
    public required int UserId { get; init; }
}
