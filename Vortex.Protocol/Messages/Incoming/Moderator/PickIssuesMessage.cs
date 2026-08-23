using System.Collections.Immutable;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Moderator;

public record PickIssuesMessage : IMessageEvent
{
    public required ImmutableArray<int> IssueIds { get; init; }

    /// <summary>
    /// Set when the request came from the tool's auto-pick loop rather than a click. The server
    /// does not act on it — it echoes it back in <c>IssuePickFailedMessageComposer</c>, which is
    /// what tells the client it may try the next bundle instead of alerting the moderator.
    /// Sourced from <c>IssueManager.pickBundle(id, note, retryEnabled, retryCount)</c>.
    /// </summary>
    public required bool RetryEnabled { get; init; }

    /// <summary>
    /// How many times this loop has already retried. Echoed back untouched; the client stops at 10.
    /// </summary>
    public required int RetryCount { get; init; }

    /// <summary>Client-supplied free text (sometimes a room name, sometimes a bundling note
    /// depending on the client-side call site) — not trusted for anything server-side.</summary>
    public required string Note { get; init; }
}
