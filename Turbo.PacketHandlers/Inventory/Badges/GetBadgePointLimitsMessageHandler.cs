using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Inventory.Badges;
using Turbo.Primitives.Messages.Outgoing.Inventory.Badges;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players.Snapshots;

namespace Turbo.PacketHandlers.Inventory.Badges;

/// <summary>
/// Serves the badge tool's "points needed per level" catalog from the real achievement definitions
/// (the badge code is <c>"ACH_" + achievement name + level</c>), so the display matches actual
/// progression thresholds instead of a hardcoded config.
/// </summary>
public class GetBadgePointLimitsMessageHandler(IGrainFactory grainFactory)
    : IMessageHandler<GetBadgePointLimitsMessage>
{
    public async ValueTask HandleAsync(
        GetBadgePointLimitsMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        ImmutableArray<AchievementDefinitionSnapshot> definitions = await grainFactory
            .GetAchievementManagerGrain()
            .GetDefinitionsAsync(ct)
            .ConfigureAwait(false);

        List<BadgePointLimitGroup> groups = definitions
            .Select(definition => new BadgePointLimitGroup
            {
                BadgeCodePrefix = definition.Name,
                Levels = definition
                    .Levels.Select(level => new BadgePointLimitLevel
                    {
                        Level = level.Level,
                        Limit = level.ProgressRequirement,
                    })
                    .ToList(),
            })
            .ToList();

        await grainFactory
            .GetPlayerPresenceGrain(ctx.PlayerId)
            .SendComposerAsync(
                new BadgePointLimitsEventMessageComposer { LimitsByBadgeCodePrefix = groups }
            )
            .ConfigureAwait(false);
    }
}
