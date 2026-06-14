using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Context;
using Turbo.Messages.Registry;
using Turbo.Primitives.Messages.Incoming.Inventory.Badges;

namespace Turbo.PacketHandlers.Inventory.Badges;

public class SetActivatedBadgesMessageHandler(IDbContextFactory<TurboDbContext> dbCtxFactory)
    : IMessageHandler<SetActivatedBadgesMessage>
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;

    public async ValueTask HandleAsync(
        SetActivatedBadgesMessage message,
        MessageContext ctx,
        CancellationToken ct
    )
    {
        if (ctx.PlayerId <= 0)
            return;

        await using var dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        await dbCtx
            .PlayerBadges.Where(b => b.PlayerEntityId == ctx.PlayerId && b.SlotId > 0)
            .ExecuteUpdateAsync(up => up.SetProperty(b => b.SlotId, 0), ct)
            .ConfigureAwait(false);

        foreach (var (slotId, badgeCode) in message.Slots)
        {
            if (slotId <= 0 || string.IsNullOrWhiteSpace(badgeCode))
                continue;

            await dbCtx
                .PlayerBadges.Where(b =>
                    b.PlayerEntityId == ctx.PlayerId && b.BadgeCode == badgeCode
                )
                .ExecuteUpdateAsync(up => up.SetProperty(b => b.SlotId, slotId), ct)
                .ConfigureAwait(false);
        }
    }
}
