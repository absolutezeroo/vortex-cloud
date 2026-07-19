using System.Collections.Immutable;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Moderator;

public record CloseIssuesMessage : IMessageEvent
{
    /// <summary>1 = useless, 2 = sanctioned/abusive, 3+ = resolved (confirmed against the client's
    /// own CallForHelpManager.getCloseReasonKey mapping).</summary>
    public required int CloseReason { get; init; }

    public required ImmutableArray<int> IssueIds { get; init; }
}
