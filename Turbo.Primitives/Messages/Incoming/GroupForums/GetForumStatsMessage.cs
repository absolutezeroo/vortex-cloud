using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.GroupForums;

public record GetForumStatsMessage : IMessageEvent
{
    public required int GroupId { get; init; }
}
