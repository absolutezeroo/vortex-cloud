using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Users;

public record GetRelationshipStatusInfoMessage : IMessageEvent
{
    public required int UserId { get; init; }
}
