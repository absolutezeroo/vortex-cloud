using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Quest;
using Turbo.Primitives.Messages.Outgoing.Quest;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Quests.Snapshots;

namespace Turbo.PacketHandlers.Quest;

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
