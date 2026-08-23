using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Help;

/// <summary>A line of chat inside a guide session.</summary>
public record GuideSessionMessageMessage : IMessageEvent
{
    public required string Message { get; init; }
}
