using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.GroupForums;

/// <summary>
/// One forum's read marker as the client reports it. AS3-verified against
/// <c>UpdateForumReadMarkerMessageComposer.add(groupId, lastReadMessageId, markAllRead)</c>.
/// <c>ReadMessageCount</c> is a COUNT, not a post id: the client derives it as
/// <c>totalMessages - unreadMessages</c>.
/// </summary>
[GenerateSerializer, Immutable]
public readonly record struct ForumReadMarkerUpdate(
    [property: Id(0)] int GroupId,
    [property: Id(1)] int ReadMessageCount,
    [property: Id(2)] bool MarkAllRead
);

/// <summary>
/// Sent when the player leaves a forum view, or uses "mark all forums as read" — the client batches
/// one entry per forum into a single packet.
/// </summary>
public record UpdateForumReadMarkerMessage : IMessageEvent
{
    public required ImmutableArray<ForumReadMarkerUpdate> Markers { get; init; }
}
