using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.GroupForums;

public record GetForumStatsMessage : IMessageEvent
{
    public required int GroupId { get; init; }
}
