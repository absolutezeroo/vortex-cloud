using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Orleans;
using Turbo.Messages.Registry;
using Turbo.PacketHandlers.Configuration;
using Turbo.Primitives.Messages.Incoming.Inventory.Badges;
using Turbo.Primitives.Messages.Outgoing.Inventory.Badges;
using Turbo.Primitives.Orleans;

namespace Turbo.PacketHandlers.Inventory.Badges;

public class GetBadgePointLimitsMessageHandler(
    IGrainFactory grainFactory,
    IOptions<BadgeConfig> badgeConfig
) : IMessageHandler<GetBadgePointLimitsMessage>
{
    private readonly BadgeConfig _badgeConfig = badgeConfig.Value;

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

        List<BadgePointLimitGroup> groups = _badgeConfig
            .LimitsByCategory.Select(kv => new BadgePointLimitGroup
            {
                BadgeCodePrefix = kv.Key,
                Levels = kv
                    .Value.Select(
                        (limit, index) =>
                            new BadgePointLimitLevel { Level = index + 1, Limit = limit }
                    )
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
