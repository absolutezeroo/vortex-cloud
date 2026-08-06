using System.Collections.Immutable;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Help;

/// <summary>Reporting someone from a private conversation. Same shape as
/// <see cref="CallForHelpMessage"/> minus the room id — an IM has no room.</summary>
public record CallForHelpFromIMMessage : IMessageEvent
{
    public required string Message { get; init; }
    public required int TopicId { get; init; }
    public required int ReportedUserId { get; init; }
    public required ImmutableArray<CfhEvidenceLine> Evidence { get; init; }
}
