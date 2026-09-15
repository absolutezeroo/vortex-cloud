using System.Collections.Immutable;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Help;

public record CallForHelpMessage : IMessageEvent
{
    public required string Message { get; init; }

    public required int TopicId { get; init; }

    public required int ReportedUserId { get; init; }

    public required int RoomId { get; init; }

    public required ImmutableArray<CfhEvidenceLine> Evidence { get; init; }

    /// <summary>
    /// The reporter's own name, from the `help_message_name` input. Empty except when the chosen
    /// topic is in the client's `_unlawfulCategories` list, where the report form asks the reporter
    /// to identify themselves (TopicsFlowHelpController.as:478-483). Not the reported user.
    /// </summary>
    public string ReporterName { get; init; } = string.Empty;

    /// <summary>The reporter's contact email, from `help_message_email`. Same condition as
    /// <see cref="ReporterName"/>.</summary>
    public string ReporterEmail { get; init; } = string.Empty;
}
