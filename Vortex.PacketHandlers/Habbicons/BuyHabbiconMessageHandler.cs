using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Habbicons;

namespace Vortex.PacketHandlers.Habbicons;

/// <summary>
/// Buy one Habbicon. The price is never on the wire -- the client sends an id and the grain reads
/// what this hotel charges for it.
/// </summary>
public class BuyHabbiconMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<BuyHabbiconMessage>
{
    public async ValueTask HandleAsync(
        BuyHabbiconMessage message,
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
            .BuyHabbiconAsync(message.HabbiconId, ct)
            .ConfigureAwait(false);
    }
}
