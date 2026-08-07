using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Help;

/// <summary>A guardian taking the offered chat review, or passing on it.</summary>
public record ChatReviewGuideDecidesOnOfferMessage : IMessageEvent
{
    public required bool Accepted { get; init; }
}
