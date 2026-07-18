using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Quest;
using Turbo.Primitives.Messages.Outgoing.Quest;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Quests.Snapshots;

namespace Turbo.PacketHandlers.Quest;

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
