using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Habbicons;

namespace Vortex.PacketHandlers.Habbicons;

/// <summary>The hub asking for the whole shop. The grain resolves it against this player's ownership.</summary>
public class GetHabbiconShopDataMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetHabbiconShopDataMessage>
{
    public async ValueTask HandleAsync(
        GetHabbiconShopDataMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await grainFactory
            .GetPlayerHabbiconGrain(ctx.PlayerId)
            .PushInventoryAsync(ct)
            .ConfigureAwait(false);
    }
}
