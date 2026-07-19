using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Furniture.Enums;

namespace Vortex.Catalog;

/// <summary>
/// Answers "is a limited edition item available right now, or when's the next one" for the landing
/// page's NextLimitedRareCountdownWidget-equivalent. Reads catalog_ltd_series directly rather than
/// through the cached ICatalogSnapshotProvider, since scheduling (StartsAt/EndsAt) is time-sensitive
/// in a way a periodically-refreshed snapshot isn't a good fit for.
/// </summary>
internal sealed class LtdScheduleService(IDbContextFactory<VortexDbContext> dbContextFactory)
    : ILtdScheduleService
{
    private readonly IDbContextFactory<VortexDbContext> _dbContextFactory = dbContextFactory;

    public async Task<LimitedOfferAppearanceSnapshot> GetNextAppearanceAsync(
        CancellationToken ct = default
    )
    {
        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        DateTime now = DateTime.UtcNow;

        var active = await dbCtx
            .LtdSeries.AsNoTracking()
            .Where(s =>
                s.IsActive
                && s.DeletedAt == null
                && (s.StartsAt == null || s.StartsAt <= now)
                && (s.EndsAt == null || s.EndsAt > now)
            )
            .Select(s => new
            {
                s.CatalogProductEntity.ProductType,
                s.CatalogProductEntity.Offer.Id,
                s.CatalogProductEntity.Offer.CatalogPageEntityId,
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (active is not null)
        {
            return new LimitedOfferAppearanceSnapshot(
                0,
                active.CatalogPageEntityId,
                active.Id,
                active.ProductType.ToLegacyString()
            );
        }

        DateTime? nextStart = await dbCtx
            .LtdSeries.AsNoTracking()
            .Where(s => s.IsActive && s.DeletedAt == null && s.StartsAt > now)
            .OrderBy(s => s.StartsAt)
            .Select(s => s.StartsAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (nextStart is null)
        {
            return new LimitedOfferAppearanceSnapshot(0, -1, -1, string.Empty);
        }

        int appearsInSeconds = (int)Math.Max(0, (nextStart.Value - now).TotalSeconds);

        return new LimitedOfferAppearanceSnapshot(appearsInSeconds, -1, -1, string.Empty);
    }

    public async Task<CatalogPageExpirySnapshot> GetPageWithEarliestExpiryAsync(
        CancellationToken ct = default
    )
    {
        await using VortexDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        DateTime now = DateTime.UtcNow;

        var soonestExpiring = await dbCtx
            .LtdSeries.AsNoTracking()
            .Where(s => s.IsActive && s.DeletedAt == null && s.EndsAt != null && s.EndsAt > now)
            .OrderBy(s => s.EndsAt)
            .Select(s => new { s.EndsAt, s.CatalogProductEntity.Offer.Page.Name })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (soonestExpiring is null || string.IsNullOrEmpty(soonestExpiring.Name))
        {
            return new CatalogPageExpirySnapshot(string.Empty, 0, string.Empty);
        }

        int secondsToExpiry = (int)Math.Max(0, (soonestExpiring.EndsAt!.Value - now).TotalSeconds);

        return new CatalogPageExpirySnapshot(
            soonestExpiring.Name,
            secondsToExpiry,
            soonestExpiring.Name
        );
    }
}
