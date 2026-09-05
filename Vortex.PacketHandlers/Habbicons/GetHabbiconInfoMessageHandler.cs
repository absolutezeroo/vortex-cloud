using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Habbicons;

namespace Vortex.PacketHandlers.Habbicons;

public class GetHabbiconInfoMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetHabbiconInfoMessage>
{
    public async ValueTask HandleAsync(
        GetHabbiconInfoMessage message,
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
            .SendHabbiconInfoAsync(message.HabbiconId, ct)
            .ConfigureAwait(false);
    }
}
