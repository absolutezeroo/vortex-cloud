using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Habbicons;

namespace Vortex.PacketHandlers.Habbicons;

/// <summary>Buy every entry of a set the player is still missing, at the set price.</summary>
public class BuyHabbiconCollectionMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<BuyHabbiconCollectionMessage>
{
    public async ValueTask HandleAsync(
        BuyHabbiconCollectionMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.CollectionId <= 0)
        {
            return;
        }

        await grainFactory
            .GetPlayerHabbiconGrain(ctx.PlayerId)
            .BuyCollectionAsync(message.CollectionId, ct)
            .ConfigureAwait(false);
    }
}
