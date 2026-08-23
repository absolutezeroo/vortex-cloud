using Orleans;

namespace Vortex.Primitives.Groups.Snapshots;

/// <summary>
/// One forum's read marker. AS3-verified against
/// <c>UpdateForumReadMarkerMessageComposer.add(groupId, lastReadMessageId, markAllRead)</c>.
/// <c>ReadMessageCount</c> is a COUNT, not a post id: the client derives it as
/// <c>totalMessages - unreadMessages</c>.
///
/// Lives on the contracts side rather than in the message that carries it, because
/// <c>IGroupDirectoryGrain.UpdateForumReadMarkersAsync</c> takes it: a grain contract typed in a
/// wire record is what stops the hub from being protocol-free. The message referencing a domain
/// type is the allowed direction.
/// </summary>
[GenerateSerializer, Immutable]
public readonly record struct ForumReadMarkerUpdate(
    [property: Id(0)] int GroupId,
    [property: Id(1)] int ReadMessageCount,
    [property: Id(2)] bool MarkAllRead
);
