using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Users;

public record RemoveAdminRightsFromMemberMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required int UserId { get; init; }
}
