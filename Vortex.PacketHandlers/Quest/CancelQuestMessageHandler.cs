using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Quest;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Quest;

public class CancelQuestMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<CancelQuestMessage>
{
    public async ValueTask HandleAsync(
        CancelQuestMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await grainFactory.GetPlayerQuestGrain(ctx.PlayerId).CancelAsync(ct).ConfigureAwait(false);
    }
}
