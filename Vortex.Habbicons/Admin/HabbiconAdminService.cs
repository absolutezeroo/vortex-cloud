using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Habbicons;
using Vortex.Primitives.Habbicons;
using Vortex.Primitives.Habbicons.Admin;
using Vortex.Primitives.Habbicons.Snapshots;
using Vortex.Primitives.Orleans;

namespace Vortex.Habbicons.Admin;

/// <summary>
/// Content CRUD for Habbicons, plus the two per-player operations an operator needs.
/// </summary>
/// <remarks>
/// Every write reloads <see cref="HabbiconCatalog"/> before returning, so the next request already
/// sees the change — the catalog is a cache, and a cache nobody invalidates is how an operator ends
/// up editing a row and watching the hotel ignore it.
/// </remarks>
internal sealed class HabbiconAdminService(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    HabbiconCatalog catalog,
    IGrainFactory grainFactory,
    ILogger<HabbiconAdminService> logger
) : IHabbiconAdminService
{
    public async Task<HabbiconAdminResult> CreateCollectionAsync(
        HabbiconCollectionSpec spec,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(spec.Code))
        {
            return HabbiconAdminResult.Fail("code_required");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        if (
            await db
                .HabbiconCollections.AnyAsync(c => c.Code == spec.Code && c.DeletedAt == null, ct)
                .ConfigureAwait(false)
        )
        {
            return HabbiconAdminResult.Fail("code_taken");
        }

        HabbiconCollectionEntity row = new()
        {
            Code = spec.Code,
            SortOrder = spec.SortOrder,
            Enabled = spec.Enabled,
            Hidden = spec.Hidden,
            AvailableFrom = spec.AvailableFrom,
            AvailableUntil = spec.AvailableUntil,
            PriceCredits = spec.PriceCredits,
            PriceActivityPoints = spec.PriceActivityPoints,
            ActivityPointType = spec.ActivityPointType,
            CampaignCode = spec.CampaignCode,
        };

        db.HabbiconCollections.Add(row);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await catalog.ReloadAsync(ct).ConfigureAwait(false);

        return HabbiconAdminResult.Ok(row.Id);
    }

    public async Task<HabbiconAdminResult> UpdateCollectionAsync(
        int collectionId,
        HabbiconCollectionSpec spec,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        HabbiconCollectionEntity? row = await db
            .HabbiconCollections.FirstOrDefaultAsync(
                c => c.Id == collectionId && c.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

        if (row is null)
        {
            return HabbiconAdminResult.Fail("not_found");
        }

        if (
            row.Code != spec.Code
            && await db
                .HabbiconCollections.AnyAsync(c => c.Code == spec.Code && c.DeletedAt == null, ct)
                .ConfigureAwait(false)
        )
        {
            return HabbiconAdminResult.Fail("code_taken");
        }

        row.Code = spec.Code;
        row.SortOrder = spec.SortOrder;
        row.Enabled = spec.Enabled;
        row.Hidden = spec.Hidden;
        row.AvailableFrom = spec.AvailableFrom;
        row.AvailableUntil = spec.AvailableUntil;
        row.PriceCredits = spec.PriceCredits;
        row.PriceActivityPoints = spec.PriceActivityPoints;
        row.ActivityPointType = spec.ActivityPointType;
        row.CampaignCode = spec.CampaignCode;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await catalog.ReloadAsync(ct).ConfigureAwait(false);

        return HabbiconAdminResult.Ok(row.Id);
    }

    public async Task<HabbiconAdminResult> DeleteCollectionAsync(
        int collectionId,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        List<int> memberIds = await db
            .Habbicons.Where(h => h.HabbiconCollectionEntityId == collectionId)
            .Select(h => h.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // Refused rather than cascaded. A player's ownership row would survive the definition and
        // render as a hole in their album that no operator could explain.
        if (
            memberIds.Count > 0
            && await db
                .PlayerHabbicons.AnyAsync(p => memberIds.Contains(p.HabbiconEntityId), ct)
                .ConfigureAwait(false)
        )
        {
            return HabbiconAdminResult.Fail("owned_by_players");
        }

        HabbiconCollectionEntity? row = await db
            .HabbiconCollections.FirstOrDefaultAsync(c => c.Id == collectionId, ct)
            .ConfigureAwait(false);

        if (row is null)
        {
            return HabbiconAdminResult.Fail("not_found");
        }

        // Tracked deletes so the members and their collection go in one commit; a partial delete
        // would leave Habbicons pointing at a collection that is no longer there.
        db.Habbicons.RemoveRange(
            await db
                .Habbicons.Where(h => h.HabbiconCollectionEntityId == collectionId)
                .ToListAsync(ct)
                .ConfigureAwait(false)
        );
        db.HabbiconCollections.Remove(row);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await catalog.ReloadAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Deleted Habbicon collection {CollectionId} ({Code}) and its {Count} member(s).",
            collectionId,
            row.Code,
            memberIds.Count
        );

        return HabbiconAdminResult.Ok(collectionId);
    }

    public async Task<HabbiconAdminResult> CreateHabbiconAsync(
        HabbiconSpec spec,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(spec.Code))
        {
            return HabbiconAdminResult.Fail("code_required");
        }

        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        if (
            !await db
                .HabbiconCollections.AnyAsync(
                    c => c.Id == spec.CollectionId && c.DeletedAt == null,
                    ct
                )
                .ConfigureAwait(false)
        )
        {
            return HabbiconAdminResult.Fail("collection_not_found");
        }

        if (
            await db
                .Habbicons.AnyAsync(h => h.Code == spec.Code && h.DeletedAt == null, ct)
                .ConfigureAwait(false)
        )
        {
            return HabbiconAdminResult.Fail("code_taken");
        }

        if (spec.IsCollectionReward && await HasRewardAsync(db, spec.CollectionId, null, ct))
        {
            return HabbiconAdminResult.Fail("collection_already_has_reward");
        }

        HabbiconEntity row = new()
        {
            Code = spec.Code,
            HabbiconCollectionEntityId = spec.CollectionId,
            SortOrder = spec.SortOrder,
            IsCollectionReward = spec.IsCollectionReward,
            PriceCredits = spec.PriceCredits,
            PriceActivityPoints = spec.PriceActivityPoints,
            ActivityPointType = spec.ActivityPointType,
            Enabled = spec.Enabled,
            AvailableFrom = spec.AvailableFrom,
            AvailableUntil = spec.AvailableUntil,
        };

        db.Habbicons.Add(row);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await catalog.ReloadAsync(ct).ConfigureAwait(false);

        return HabbiconAdminResult.Ok(row.Id);
    }

    public async Task<HabbiconAdminResult> UpdateHabbiconAsync(
        int habbiconId,
        HabbiconSpec spec,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        HabbiconEntity? row = await db
            .Habbicons.FirstOrDefaultAsync(h => h.Id == habbiconId && h.DeletedAt == null, ct)
            .ConfigureAwait(false);

        if (row is null)
        {
            return HabbiconAdminResult.Fail("not_found");
        }

        if (
            row.Code != spec.Code
            && await db
                .Habbicons.AnyAsync(h => h.Code == spec.Code && h.DeletedAt == null, ct)
                .ConfigureAwait(false)
        )
        {
            return HabbiconAdminResult.Fail("code_taken");
        }

        if (spec.IsCollectionReward && await HasRewardAsync(db, spec.CollectionId, habbiconId, ct))
        {
            return HabbiconAdminResult.Fail("collection_already_has_reward");
        }

        row.Code = spec.Code;
        row.HabbiconCollectionEntityId = spec.CollectionId;
        row.SortOrder = spec.SortOrder;
        row.IsCollectionReward = spec.IsCollectionReward;
        row.PriceCredits = spec.PriceCredits;
        row.PriceActivityPoints = spec.PriceActivityPoints;
        row.ActivityPointType = spec.ActivityPointType;
        row.Enabled = spec.Enabled;
        row.AvailableFrom = spec.AvailableFrom;
        row.AvailableUntil = spec.AvailableUntil;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await catalog.ReloadAsync(ct).ConfigureAwait(false);

        return HabbiconAdminResult.Ok(row.Id);
    }

    public async Task<HabbiconAdminResult> DeleteHabbiconAsync(int habbiconId, CancellationToken ct)
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        if (
            await db
                .PlayerHabbicons.AnyAsync(p => p.HabbiconEntityId == habbiconId, ct)
                .ConfigureAwait(false)
        )
        {
            return HabbiconAdminResult.Fail("owned_by_players");
        }

        HabbiconEntity? row = await db
            .Habbicons.FirstOrDefaultAsync(h => h.Id == habbiconId, ct)
            .ConfigureAwait(false);

        if (row is null)
        {
            return HabbiconAdminResult.Fail("not_found");
        }

        db.Habbicons.Remove(row);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await catalog.ReloadAsync(ct).ConfigureAwait(false);

        return HabbiconAdminResult.Ok(habbiconId);
    }

    public async Task<IReadOnlyList<HabbiconCollectionStats>> GetCollectionStatsAsync(
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        // One grouped query rather than a query per collection: a hotel with forty sets would
        // otherwise open forty round trips to draw one list.
        Dictionary<int, int> ownersByCollection = await db
            .PlayerHabbicons.Where(p => p.Habbicon!.DeletedAt == null)
            .GroupBy(p => p.Habbicon!.HabbiconCollectionEntityId)
            .Select(g => new
            {
                CollectionId = g.Key,
                Owners = g.Select(x => x.PlayerEntityId).Distinct().Count(),
            })
            .ToDictionaryAsync(x => x.CollectionId, x => x.Owners, ct)
            .ConfigureAwait(false);

        List<HabbiconCollectionStats> stats = [];

        foreach (HabbiconCollectionSnapshot collection in catalog.Collections)
        {
            int completed = await CountCompletionsAsync(db, collection, ct).ConfigureAwait(false);

            stats.Add(
                new HabbiconCollectionStats(
                    collection.CollectionId,
                    collection.Code,
                    collection.Entries.Length,
                    collection.RewardHabbicon is not null,
                    ownersByCollection.GetValueOrDefault(collection.CollectionId),
                    completed
                )
            );
        }

        return stats;
    }

    public async Task<IReadOnlyList<PlayerHabbiconAdminRow>> GetPlayerHabbiconsAsync(
        int playerId,
        CancellationToken ct
    )
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        List<PlayerHabbiconEntity> rows = await db
            .PlayerHabbicons.AsNoTracking()
            .Where(p => p.PlayerEntityId == playerId && p.DeletedAt == null)
            .OrderByDescending(p => p.AcquiredAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return
        [
            .. rows.Select(r => new PlayerHabbiconAdminRow(
                r.HabbiconEntityId,
                catalog.TryGetHabbicon(r.HabbiconEntityId, out HabbiconDefinitionSnapshot? d)
                    ? d.Code
                    : string.Empty,
                catalog.TryGetHabbicon(r.HabbiconEntityId, out HabbiconDefinitionSnapshot? c)
                    ? c.CollectionId
                    : 0,
                r.State,
                r.Source,
                r.AcquiredAt,
                r.LastUsedAt
            )),
        ];
    }

    public async Task<HabbiconAdminResult> GrantAsync(
        int playerId,
        int habbiconId,
        CancellationToken ct
    )
    {
        // Through the grain, never the table: it caches ownership, so a row written behind its back
        // is invisible to a player who is online and is clobbered by the grain's next write.
        HabbiconGrantResult result = await grainFactory
            .GetPlayerHabbiconGrain(playerId)
            .GrantAsync(habbiconId, HabbiconSource.AdminGrant, ct)
            .ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return HabbiconAdminResult.Fail("habbicon_not_found");
        }

        logger.LogInformation(
            "Operator granted Habbicon {HabbiconId} to player {PlayerId} (new={WasNew}).",
            habbiconId,
            playerId,
            result.WasNew
        );

        return HabbiconAdminResult.Ok(habbiconId);
    }

    public async Task<HabbiconAdminResult> RevokeAsync(
        int playerId,
        int habbiconId,
        CancellationToken ct
    )
    {
        bool removed = await grainFactory
            .GetPlayerHabbiconGrain(playerId)
            .RevokeAsync(habbiconId, ct)
            .ConfigureAwait(false);

        return removed ? HabbiconAdminResult.Ok(habbiconId) : HabbiconAdminResult.Fail("not_owned");
    }

    public async Task<HabbiconContentReport> ValidateAsync(CancellationToken ct)
    {
        await using VortexDbContext db = await dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        List<HabbiconContentProblem> problems = [];

        List<HabbiconEntity> all = await db
            .Habbicons.AsNoTracking()
            .Where(h => h.DeletedAt == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        List<HabbiconCollectionEntity> collections = await db
            .HabbiconCollections.AsNoTracking()
            .Where(c => c.DeletedAt == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        HashSet<int> collectionIds = [.. collections.Select(c => c.Id)];

        foreach (
            IGrouping<string, HabbiconEntity> duplicate in all.GroupBy(h => h.Code)
                .Where(g => g.Count() > 1)
        )
        {
            problems.Add(
                new HabbiconContentProblem(
                    "duplicate_habbicon_code",
                    $"{duplicate.Key} is used by {duplicate.Count()} Habbicons"
                )
            );
        }

        foreach (
            IGrouping<string, HabbiconCollectionEntity> duplicate in collections
                .GroupBy(c => c.Code)
                .Where(g => g.Count() > 1)
        )
        {
            problems.Add(
                new HabbiconContentProblem(
                    "duplicate_collection_code",
                    $"{duplicate.Key} is used by {duplicate.Count()} collections"
                )
            );
        }

        foreach (
            HabbiconEntity orphan in all.Where(h =>
                !collectionIds.Contains(h.HabbiconCollectionEntityId)
            )
        )
        {
            problems.Add(
                new HabbiconContentProblem(
                    "habbicon_without_collection",
                    $"{orphan.Code} points at collection {orphan.HabbiconCollectionEntityId}, which does not exist"
                )
            );
        }

        foreach (HabbiconCollectionEntity collection in collections)
        {
            List<HabbiconEntity> members =
            [
                .. all.Where(h => h.HabbiconCollectionEntityId == collection.Id),
            ];

            int rewards = members.Count(h => h.IsCollectionReward);

            if (rewards > 1)
            {
                problems.Add(
                    new HabbiconContentProblem(
                        "multiple_collection_rewards",
                        $"{collection.Code} has {rewards} bonus Habbicons; the catalog will pick one arbitrarily"
                    )
                );
            }

            if (members.Count(h => !h.IsCollectionReward) == 0)
            {
                // A set with no ordinary entries can never be completed, so its bonus can never be
                // claimed. That is content nobody can finish rather than a runtime error, which is
                // exactly what a validator is for.
                problems.Add(
                    new HabbiconContentProblem(
                        "empty_collection",
                        $"{collection.Code} has no entries, so it can never be completed"
                    )
                );
            }
        }

        return new HabbiconContentReport(problems);
    }

    private static Task<bool> HasRewardAsync(
        VortexDbContext db,
        int collectionId,
        int? excludingId,
        CancellationToken ct
    ) =>
        db.Habbicons.AnyAsync(
            h =>
                h.HabbiconCollectionEntityId == collectionId
                && h.IsCollectionReward
                && h.DeletedAt == null
                && (excludingId == null || h.Id != excludingId),
            ct
        );

    /// <summary>
    /// How many players own every ordinary entry of <paramref name="collection"/>. Counted in the
    /// database: a set of 20 across 10,000 players is not something to pull into memory.
    /// </summary>
    private static async Task<int> CountCompletionsAsync(
        VortexDbContext db,
        HabbiconCollectionSnapshot collection,
        CancellationToken ct
    )
    {
        if (collection.Entries.IsDefaultOrEmpty)
        {
            return 0;
        }

        int[] entryIds = [.. collection.Entries.Select(e => e.HabbiconId)];

        return await db
            .PlayerHabbicons.Where(p =>
                entryIds.Contains(p.HabbiconEntityId) && p.DeletedAt == null
            )
            .GroupBy(p => p.PlayerEntityId)
            .CountAsync(g => g.Count() == entryIds.Length, ct)
            .ConfigureAwait(false);
    }
}
