using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Help;

/// <summary>Reporting a whole guild-forum thread.</summary>
public record CallForHelpFromForumThreadMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required int ThreadId { get; init; }
    public required int TopicId { get; init; }
    public required string Message { get; init; }
}
