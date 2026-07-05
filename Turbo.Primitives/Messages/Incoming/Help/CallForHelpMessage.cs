using System.Collections.Immutable;
using Turbo.Primitives.Moderation;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Help;

public record CallForHelpMessage : IMessageEvent
{
    public required string Message { get; init; }

    public required int TopicId { get; init; }

    public required int ReportedUserId { get; init; }

    public required int RoomId { get; init; }

    public required ImmutableArray<CfhEvidenceLine> Evidence { get; init; }

    /// <summary>Always empty in every observed client call site — parsed to keep the wire read in
    /// sync, not currently used for anything.</summary>
    public string Extra1 { get; init; } = string.Empty;

    /// <summary>See <see cref="Extra1"/>.</summary>
    public string Extra2 { get; init; } = string.Empty;
}
