using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Habbicons;

namespace Vortex.PacketHandlers.Habbicons;

public class UnfavouriteHabbiconMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<UnfavouriteHabbiconMessage>
{
    public async ValueTask HandleAsync(
        UnfavouriteHabbiconMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0 || message.HabbiconId <= 0)
        {
            return;
        }

        await grainFactory
            .GetPlayerHabbiconGrain(ctx.PlayerId)
            .SetFavouriteAsync(message.HabbiconId, favourite: false, ct)
            .ConfigureAwait(false);
    }
}
