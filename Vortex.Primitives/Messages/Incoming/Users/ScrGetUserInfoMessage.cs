using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Users;

public record ScrGetUserInfoMessage : IMessageEvent
{
    public required string ProductName { get; init; }
}
