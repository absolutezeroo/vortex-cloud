using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Users;

public record UpdateGuildIdentityMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
}
