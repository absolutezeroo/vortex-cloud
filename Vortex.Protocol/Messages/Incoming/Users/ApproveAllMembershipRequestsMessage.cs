using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Users;

public record ApproveAllMembershipRequestsMessage : IMessageEvent
{
    public required int GroupId { get; init; }
}
