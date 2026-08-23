using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Quests.Snapshots;
using Vortex.Protocol.Messages.Incoming.Quest;
using Vortex.Protocol.Messages.Outgoing.Quest;

namespace Vortex.PacketHandlers.Quest;

public class OpenQuestTrackerMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<OpenQuestTrackerMessage>
{
    public async ValueTask HandleAsync(
        OpenQuestTrackerMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        QuestSnapshot? tracked = await grainFactory
            .GetPlayerQuestGrain(ctx.PlayerId)
            .GetTrackedQuestAsync(ct)
            .ConfigureAwait(false);

        // Nothing accepted to track — leave the tracker closed.
        if (tracked is null)
        {
            return;
        }

        await grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(new QuestMessageComposer { Quest = tracked })
            .ConfigureAwait(false);
    }
}
