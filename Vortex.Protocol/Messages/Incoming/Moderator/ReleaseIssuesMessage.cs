using System.Collections.Immutable;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Moderator;

public record ReleaseIssuesMessage : IMessageEvent
{
    public required ImmutableArray<int> IssueIds { get; init; }
}
