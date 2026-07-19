using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Users;

public record AddAdminRightsToMemberMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required int UserId { get; init; }
}
