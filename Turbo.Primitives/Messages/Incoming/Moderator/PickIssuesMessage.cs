using System.Collections.Immutable;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Moderator;

public record PickIssuesMessage : IMessageEvent
{
    public required ImmutableArray<int> IssueIds { get; init; }

    public required bool AutoHandle { get; init; }

    public required int RoomId { get; init; }

    /// <summary>Client-supplied free text (sometimes a room name, sometimes a bundling note
    /// depending on the client-side call site) — not trusted for anything server-side.</summary>
    public required string Note { get; init; }
}
