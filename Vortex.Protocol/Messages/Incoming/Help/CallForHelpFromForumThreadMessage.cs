using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Help;

/// <summary>Reporting a whole guild-forum thread.</summary>
public record CallForHelpFromForumThreadMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required int ThreadId { get; init; }
    public required int TopicId { get; init; }
    public required string Message { get; init; }

    /// <summary>See <see cref="CallForHelpMessage.ReporterName"/>.</summary>
    public string ReporterName { get; init; } = string.Empty;

    /// <summary>See <see cref="CallForHelpMessage.ReporterEmail"/>.</summary>
    public string ReporterEmail { get; init; } = string.Empty;
}
