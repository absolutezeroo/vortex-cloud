using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Quest;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Quest;

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
