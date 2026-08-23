using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Users;

public record UpdateGuildIdentityMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
}
