using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.RewardTracks;

namespace Vortex.PacketHandlers.RewardTracks;

/// <summary>
/// Claim one prize. Both ids are content ids the client is echoing back; the grain re-resolves them
/// and re-checks the points, the premium entitlement and the claim window before anything moves.
/// </summary>
public class ClaimRewardTrackPrizeMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ClaimRewardTrackPrizeMessage>
{
    public async ValueTask HandleAsync(
        ClaimRewardTrackPrizeMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (
            ctx.PlayerId <= 0
            || string.IsNullOrWhiteSpace(message.TrackId)
            || string.IsNullOrWhiteSpace(message.PrizeId)
        )
        {
            return;
        }

        await grainFactory
            .GetPlayerRewardTrackGrain(ctx.PlayerId)
            .ClaimPrizeAsync(message.TrackId, message.PrizeId, ct)
            .ConfigureAwait(false);
    }
}
