using System.Collections.Immutable;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Moderator;

public record ReleaseIssuesMessage : IMessageEvent
{
    public required ImmutableArray<int> IssueIds { get; init; }
}
