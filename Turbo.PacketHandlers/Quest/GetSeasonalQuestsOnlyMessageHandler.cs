using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Quest;
using Turbo.Primitives.Messages.Outgoing.Quest;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Quests.Snapshots;

namespace Turbo.PacketHandlers.Quest;

public class GetSeasonalQuestsOnlyMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetSeasonalQuestsOnlyMessage>
{
    public async ValueTask HandleAsync(
        GetSeasonalQuestsOnlyMessage message,
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
            .GetSeasonalQuestsAsync(ct)
            .ConfigureAwait(false);

        await grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(new SeasonalQuestsMessageComposer { Quests = list.Quests })
            .ConfigureAwait(false);
    }
}
