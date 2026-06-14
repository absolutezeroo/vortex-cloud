using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Users;

public record GetRelationshipStatusInfoMessage : IMessageEvent
{
    public required int UserId { get; init; }
}
