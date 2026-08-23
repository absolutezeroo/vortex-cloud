using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Messages.Registry;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Snapshots;
using Vortex.Protocol.Messages.Incoming.Inventory.Achievements;
using Vortex.Protocol.Messages.Outgoing.Inventory.Achievements;

namespace Vortex.PacketHandlers.Inventory.Achievements;

public class GetAchievementsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetAchievementsMessage>
{
    public async ValueTask HandleAsync(
        GetAchievementsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        AchievementListSnapshot achievements = await grainFactory
            .GetPlayerAchievementGrain(ctx.PlayerId)
            .GetAchievementsAsync(ct)
            .ConfigureAwait(false);

        await grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(
                new AchievementsEventMessageComposer
                {
                    Achievements = achievements.Achievements,
                    DefaultCategory = achievements.DefaultCategory,
                }
            )
            .ConfigureAwait(false);
    }
}
