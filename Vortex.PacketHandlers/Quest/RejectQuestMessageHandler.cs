using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Quest;
using Vortex.Primitives.Orleans;

namespace Vortex.PacketHandlers.Quest;

public class RejectQuestMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<RejectQuestMessage>
{
    public async ValueTask HandleAsync(
        RejectQuestMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await grainFactory
            .GetPlayerQuestGrain(ctx.PlayerId)
            .RejectAsync(message.QuestId, ct)
            .ConfigureAwait(false);
    }
}
