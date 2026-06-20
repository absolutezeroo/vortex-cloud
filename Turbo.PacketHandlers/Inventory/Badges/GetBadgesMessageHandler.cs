using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Context;
using Turbo.Database.Entities.Players;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Inventory.Badges;
using Turbo.Primitives.Messages.Outgoing.Inventory.Badges;
using Turbo.Primitives.Players.Snapshots;

namespace Turbo.PacketHandlers.Inventory.Badges;

public class GetBadgesMessageHandler(IDbContextFactory<TurboDbContext> dbCtxFactory)
    : IMessageHandler<GetBadgesMessage>
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;

    public async ValueTask HandleAsync(
        GetBadgesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
        {
            return;
        }

        await using TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        List<PlayerBadgeEntity> entities = await dbCtx
            .PlayerBadges.AsNoTracking()
            .Where(b => b.PlayerEntityId == (int)ctx.PlayerId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        ImmutableArray<PlayerBadgeSnapshot> badges = entities
            .Select(b => new PlayerBadgeSnapshot
            {
                SlotId = b.SlotId ?? 0,
                BadgeCode = b.BadgeCode,
            })
            .ToImmutableArray();

        await ctx.SendComposerAsync(new BadgesEventMessageComposer { Badges = badges }, ct)
            .ConfigureAwait(false);
    }
}
