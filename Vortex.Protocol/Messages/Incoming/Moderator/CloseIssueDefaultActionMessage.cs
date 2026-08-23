using System.Collections.Immutable;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Moderator;

public record CloseIssueDefaultActionMessage : IMessageEvent
{
    public required int PrimaryIssueId { get; init; }

    public required ImmutableArray<int> OtherIssueIds { get; init; }

    public required int TopicId { get; init; }
}
