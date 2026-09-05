using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.RewardTracks;

namespace Vortex.PacketHandlers.RewardTracks;

/// <summary>
/// Buy premium on one track. The price is the one this hotel published for that track, never
/// anything the client sent -- it sends a track id and nothing else.
/// </summary>
public class PurchaseRewardTrackPremiumMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<PurchaseRewardTrackPremiumMessage>
{
    public async ValueTask HandleAsync(
        PurchaseRewardTrackPremiumMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || string.IsNullOrWhiteSpace(message.TrackId))
        {
            return;
        }

        await grainFactory
            .GetPlayerRewardTrackGrain(ctx.PlayerId)
            .PurchasePremiumAsync(message.TrackId, ct)
            .ConfigureAwait(false);
    }
}
