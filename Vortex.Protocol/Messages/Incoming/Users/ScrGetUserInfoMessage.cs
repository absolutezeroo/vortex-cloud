using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Users;

public record ScrGetUserInfoMessage : IMessageEvent
{
    public required string ProductName { get; init; }
}
