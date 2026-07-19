using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Messages.Incoming.Quest;
using Vortex.Primitives.Messages.Outgoing.Quest;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Quests.Snapshots;

namespace Vortex.PacketHandlers.Quest;

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
