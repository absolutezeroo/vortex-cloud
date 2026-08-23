using System.Collections.Immutable;
using Vortex.Primitives.Groups.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.GroupForums;

/// <summary>
/// Sent when the player leaves a forum view, or uses "mark all forums as read" — the client batches
/// one entry per forum into a single packet.
/// </summary>
public record UpdateForumReadMarkerMessage : IMessageEvent
{
    public required ImmutableArray<ForumReadMarkerUpdate> Markers { get; init; }
}
