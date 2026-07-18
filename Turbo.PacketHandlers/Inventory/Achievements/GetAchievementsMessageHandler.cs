using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Inventory.Achievements;
using Turbo.Primitives.Messages.Outgoing.Inventory.Achievements;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players.Snapshots;

namespace Turbo.PacketHandlers.Inventory.Achievements;

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
