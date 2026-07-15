using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Players;
using Turbo.Primitives.Players.Grains;
using Turbo.Primitives.Players.Snapshots;

namespace Turbo.Players.Grains;

internal sealed class PlayerBadgeGrain(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    ILogger<PlayerBadgeGrain> logger
) : Grain, IPlayerBadgeGrain
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<PlayerBadgeGrain> _logger = logger;

    private int PlayerId => (int)this.GetPrimaryKeyLong();

    public async Task<ImmutableArray<PlayerBadgeSnapshot>> GetBadgesAsync(CancellationToken ct)
    {
        try
        {
            await using TurboDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<PlayerBadgeEntity> entities = await dbCtx
                .PlayerBadges.AsNoTracking()
                .Where(b => b.PlayerEntityId == PlayerId)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            return entities
                .Select(b => new PlayerBadgeSnapshot
                {
                    SlotId = b.SlotId ?? 0,
                    BadgeCode = b.BadgeCode,
                })
                .ToImmutableArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load badges for player {PlayerId}", PlayerId);
            return ImmutableArray<PlayerBadgeSnapshot>.Empty;
        }
    }

    public async Task SetActivatedBadgesAsync(
        List<(int SlotId, string BadgeCode)> slots,
        CancellationToken ct
    )
    {
        try
        {
            await using TurboDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            await dbCtx
                .PlayerBadges.Where(b => b.PlayerEntityId == PlayerId && b.SlotId > 0)
                .ExecuteUpdateAsync(up => up.SetProperty(b => b.SlotId, 0), ct)
                .ConfigureAwait(true);

            Dictionary<string, int> desiredSlotByCode = slots
                .Where(s => s.SlotId > 0 && !string.IsNullOrWhiteSpace(s.BadgeCode))
                .ToDictionary(s => s.BadgeCode, s => s.SlotId);

            if (desiredSlotByCode.Count > 0)
            {
                List<string> codes = [.. desiredSlotByCode.Keys];

                List<PlayerBadgeEntity> toActivate = await dbCtx
                    .PlayerBadges.Where(b =>
                        b.PlayerEntityId == PlayerId && codes.Contains(b.BadgeCode)
                    )
                    .ToListAsync(ct)
                    .ConfigureAwait(true);

                foreach (PlayerBadgeEntity entity in toActivate)
                {
                    entity.SlotId = desiredSlotByCode[entity.BadgeCode];
                }

                await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update badge slots for player {PlayerId}", PlayerId);
        }
    }
}
