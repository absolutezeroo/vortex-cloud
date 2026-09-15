using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Help;

/// <summary>Reporting a single post inside a guild-forum thread.</summary>
public record CallForHelpFromForumMessageMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required int ThreadId { get; init; }
    public required int PostId { get; init; }
    public required int TopicId { get; init; }
    public required string Message { get; init; }

    /// <summary>See <see cref="CallForHelpMessage.ReporterName"/> — every report variant carries
    /// this pair last, and all four forum/IM/photo variants used to drop it.</summary>
    public string ReporterName { get; init; } = string.Empty;

    /// <summary>See <see cref="CallForHelpMessage.ReporterEmail"/>.</summary>
    public string ReporterEmail { get; init; } = string.Empty;
}
