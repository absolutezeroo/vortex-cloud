using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Quest;
using Vortex.Primitives.Messages.Outgoing.Quest;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Quests.Snapshots;

namespace Vortex.PacketHandlers.Quest;

public class GetDailyQuestMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetDailyQuestMessage>
{
    public async ValueTask HandleAsync(
        GetDailyQuestMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        DailyQuestSnapshot daily = await grainFactory
            .GetPlayerQuestGrain(ctx.PlayerId)
            .GetDailyQuestAsync(ct)
            .ConfigureAwait(false);

        await grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(
                new QuestDailyMessageComposer
                {
                    Quest = daily.Quest,
                    EasyQuestCount = daily.EasyQuestCount,
                    HardQuestCount = daily.HardQuestCount,
                }
            )
            .ConfigureAwait(false);
    }
}
