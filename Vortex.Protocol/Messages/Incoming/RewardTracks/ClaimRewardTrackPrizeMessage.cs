using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.RewardTracks;

/// <summary>
/// Claim one prize (<c>RewardTrackController.claimPrize</c>). Both ids are content ids chosen by
/// whoever wrote the campaign and echoed back by the client, so neither is trusted: the grain
/// re-resolves both and re-checks the points, premium and claim window.
/// </summary>
public sealed record ClaimRewardTrackPrizeMessage : IMessageEvent
{
    public required string TrackId { get; init; }

    public required string PrizeId { get; init; }
}
