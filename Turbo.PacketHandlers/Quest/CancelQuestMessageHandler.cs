using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Quest;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Quest;

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
