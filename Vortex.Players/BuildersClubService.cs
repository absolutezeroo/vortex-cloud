using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums;

namespace Vortex.Players;

/// <summary>
/// Reads the Builders Club (SubscriptionType.BuildersClub) subscription row directly rather than
/// through PlayerGrain state -- unlike Habbo Club, there's no gift-token/kickback/badge bookkeeping
/// to carry across grain activations, so a live per-call read is simpler and just as correct.
/// </summary>
internal sealed class BuildersClubService(IDbContextFactory<TurboDbContext> dbContextFactory)
    : IBuildersClubService
{
    private readonly IDbContextFactory<TurboDbContext> _dbContextFactory = dbContextFactory;

    public async Task<BuildersClubSubscriptionSnapshot> GetSubscriptionAsync(
        int playerId,
        CancellationToken ct = default
    )
    {
        await using TurboDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        PlayerSubscriptionEntity? sub = await dbCtx
            .PlayerSubscriptions.AsNoTracking()
            .FirstOrDefaultAsync(
                s =>
                    s.PlayerEntityId == playerId
                    && s.SubscriptionType == SubscriptionType.BuildersClub,
                ct
            )
            .ConfigureAwait(false);

        bool isActive = sub is not null && sub.ExpiresAt > DateTime.UtcNow;

        if (!isActive)
        {
            return new BuildersClubSubscriptionSnapshot(false, null, 0);
        }

        int furniLimit = await dbCtx
            .BuildersClubTiers.AsNoTracking()
            .Where(t => t.Level == sub!.Level && t.DeletedAt == null)
            .Select(t => t.FurniLimit)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return new BuildersClubSubscriptionSnapshot(true, sub!.ExpiresAt, furniLimit);
    }

    public async Task<int> GetOwnedFurnitureCountAsync(int playerId, CancellationToken ct = default)
    {
        await using TurboDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        return await dbCtx
            .Furnitures.AsNoTracking()
            .CountAsync(f => f.PlayerEntityId == playerId && f.DeletedAt == null, ct)
            .ConfigureAwait(false);
    }
}
