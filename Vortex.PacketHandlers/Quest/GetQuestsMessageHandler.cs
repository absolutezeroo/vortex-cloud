using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Quest;
using Vortex.Primitives.Messages.Outgoing.Quest;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Quests.Snapshots;

namespace Vortex.PacketHandlers.Quest;

public class GetQuestsMessageHandler(IGrainFactory grainFactory) : IMessageHandler<GetQuestsMessage>
{
    public async ValueTask HandleAsync(
        GetQuestsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        QuestListSnapshot list = await grainFactory
            .GetPlayerQuestGrain(ctx.PlayerId)
            .GetQuestsAsync(openWindow: true, ct)
            .ConfigureAwait(false);

        await grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(
                new QuestsMessageComposer { Quests = list.Quests, OpenWindow = list.OpenWindow }
            )
            .ConfigureAwait(false);
    }
}
