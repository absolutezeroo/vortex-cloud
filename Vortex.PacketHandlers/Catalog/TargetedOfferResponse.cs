using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Messages.Outgoing.Catalog;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Grains;

namespace Vortex.PacketHandlers.Catalog;

/// <summary>Sends the targeted-offer response: the offer event, or the not-found event when null.</summary>
internal static class TargetedOfferResponse
{
    public static async Task SendAsync(
        IGrainFactory grainFactory,
        long playerId,
        TargetedOfferSnapshot? offer
    )
    {
        IPlayerPresenceGrain presence = grainFactory.GetPlayerPresenceGrain(playerId);

        if (offer is null)
        {
            await presence
                .SendComposerAsync(new TargetedOfferNotFoundEventMessageComposer())
                .ConfigureAwait(false);
        }
        else
        {
            await presence
                .SendComposerAsync(new TargetedOfferEventMessageComposer { Offer = offer })
                .ConfigureAwait(false);
        }
    }
}
