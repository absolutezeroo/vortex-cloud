using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Quest;
using Turbo.Primitives.Messages.Outgoing.Quest;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Quests.Snapshots;

namespace Turbo.PacketHandlers.Quest;

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
