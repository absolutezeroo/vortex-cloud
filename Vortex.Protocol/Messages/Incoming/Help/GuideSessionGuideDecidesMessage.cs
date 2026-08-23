using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Help;

/// <summary>A guide accepting or turning down the request in front of them.</summary>
public record GuideSessionGuideDecidesMessage : IMessageEvent
{
    public required bool Accepted { get; init; }
}
