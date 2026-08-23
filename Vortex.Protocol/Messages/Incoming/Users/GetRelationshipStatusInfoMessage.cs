using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Users;

public record GetRelationshipStatusInfoMessage : IMessageEvent
{
    public required int UserId { get; init; }
}
