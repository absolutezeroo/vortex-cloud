using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Protocol.Messages.Incoming.Quest;

namespace Vortex.PacketHandlers.Quest;

public class ActivateQuestMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<ActivateQuestMessage>
{
    public async ValueTask HandleAsync(
        ActivateQuestMessage message,
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
            .ActivateAsync(message.QuestId, ct)
            .ConfigureAwait(false);
    }
}
