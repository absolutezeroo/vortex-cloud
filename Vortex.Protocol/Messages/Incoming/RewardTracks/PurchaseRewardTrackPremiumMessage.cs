using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.RewardTracks;

/// <summary>Buy premium on one track (<c>RewardTrackController.purchasePremium</c>).</summary>
public sealed record PurchaseRewardTrackPremiumMessage : IMessageEvent
{
    public required string TrackId { get; init; }
}
