using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Users;

public record UpdateGuildIdentityMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
}
