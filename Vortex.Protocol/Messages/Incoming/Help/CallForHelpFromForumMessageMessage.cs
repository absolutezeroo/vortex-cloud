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
}
