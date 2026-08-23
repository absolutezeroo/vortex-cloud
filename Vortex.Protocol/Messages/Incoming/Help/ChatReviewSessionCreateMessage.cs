using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Help;

/// <summary>A player asking for a conversation to be reviewed, with what they want to say about it.</summary>
public record ChatReviewSessionCreateMessage : IMessageEvent
{
    public required string Message { get; init; }
}
