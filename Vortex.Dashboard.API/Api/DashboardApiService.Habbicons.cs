using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Habbicons;
using Vortex.Primitives.Habbicons;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// Read surface for Habbicon content and ownership. The CRUD lives in
/// <c>DashboardOperationsService.Habbicons.cs</c>; here we only read.
/// </summary>
/// <remarks>
/// Counts are aggregated straight from <c>player_habbicons</c> — ownership is the only source of
/// truth for whether a set is complete, and there is no cached completion column to read instead.
/// </remarks>
internal sealed partial class DashboardApiService
{
    /// <summary>
    /// Every collection with its members and how the hotel is doing on it: how many players own at
    /// least one entry, and how many own the lot.
    /// </summary>
    public Task<object> HabbiconCollectionsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                string search = (query["search"] ?? string.Empty).Trim();

                List<HabbiconCollectionEntity> collections = await db
                    .HabbiconCollections.AsNoTracking()
                    .Where(c => c.DeletedAt == null)
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Id)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                if (search.Length > 0)
                {
                    collections =
                    [
                        .. collections.Where(c =>
                            c.Code.Contains(search, StringComparison.OrdinalIgnoreCase)
                        ),
                    ];
                }

                List<HabbiconEntity> habbicons = await db
                    .Habbicons.AsNoTracking()
                    .Where(h => h.DeletedAt == null)
                    .OrderBy(h => h.SortOrder)
                    .ThenBy(h => h.Id)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                // One grouped query rather than one per collection: forty sets would otherwise open
                // forty round trips to draw one list.
                Dictionary<int, int> ownersByHabbicon = await db
                    .PlayerHabbicons.Where(p => p.DeletedAt == null)
                    .GroupBy(p => p.HabbiconEntityId)
                    .Select(g => new { HabbiconId = g.Key, Owners = g.Count() })
                    .ToDictionaryAsync(x => x.HabbiconId, x => x.Owners, ct)
                    .ConfigureAwait(false);

                List<object> items = [];

                foreach (HabbiconCollectionEntity collection in collections)
                {
                    List<HabbiconEntity> members =
                    [
                        .. habbicons.Where(h => h.HabbiconCollectionEntityId == collection.Id),
                    ];

                    List<HabbiconEntity> entries = [.. members.Where(h => !h.IsCollectionReward)];
                    HabbiconEntity? reward = members.FirstOrDefault(h => h.IsCollectionReward);

                    int[] entryIds = [.. entries.Select(e => e.Id)];

                    int completedBy =
                        entryIds.Length == 0
                            ? 0
                            : await db
                                .PlayerHabbicons.Where(p =>
                                    entryIds.Contains(p.HabbiconEntityId) && p.DeletedAt == null
                                )
                                .GroupBy(p => p.PlayerEntityId)
                                .CountAsync(g => g.Count() == entryIds.Length, ct)
                                .ConfigureAwait(false);

                    items.Add(
                        new
                        {
                            id = collection.Id,
                            code = collection.Code,
                            // The client renders habbicon_collection_<code>_name; showing the key
                            // beside the code saves an operator guessing what to add to the texts.
                            localizationKey = $"habbicon_collection_{collection.Code}_name",
                            sortOrder = collection.SortOrder,
                            enabled = collection.Enabled,
                            hidden = collection.Hidden,
                            availableFrom = collection.AvailableFrom,
                            availableUntil = collection.AvailableUntil,
                            priceCredits = collection.PriceCredits,
                            priceActivityPoints = collection.PriceActivityPoints,
                            activityPointType = collection.ActivityPointType,
                            campaignCode = collection.CampaignCode,
                            entryCount = entries.Count,
                            rewardHabbiconId = reward?.Id ?? 0,
                            rewardCode = reward?.Code ?? string.Empty,
                            completedBy,
                            habbicons = entries
                                .Select(h => Describe(h, ownersByHabbicon))
                                .Concat(
                                    reward is null
                                        ? []
                                        : new[] { Describe(reward, ownersByHabbicon) }
                                )
                                .ToList(),
                        }
                    );
                }

                return new { count = items.Count, items };
            },
            ct
        );

    /// <summary>
    /// One player's Habbicons, with where each came from and when they last used it. What an
    /// operator opens when somebody asks why they do or do not have something.
    /// </summary>
    public Task<object> PlayerHabbiconsAsync(int playerId, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                List<PlayerHabbiconEntity> rows = await db
                    .PlayerHabbicons.AsNoTracking()
                    .Include(p => p.Habbicon)
                    .Where(p => p.PlayerEntityId == playerId && p.DeletedAt == null)
                    .OrderByDescending(p => p.AcquiredAt)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                return new
                {
                    playerId,
                    count = rows.Count,
                    items = rows.Select(r => new
                        {
                            habbiconId = r.HabbiconEntityId,
                            code = r.Habbicon?.Code ?? string.Empty,
                            collectionId = r.Habbicon?.HabbiconCollectionEntityId ?? 0,
                            state = r.State.ToString(),
                            source = r.Source.ToString(),
                            acquiredAt = r.AcquiredAt,
                            lastUsedAt = r.LastUsedAt,
                        })
                        .ToList(),
                };
            },
            ct
        );

    /// <summary>
    /// The acquisition sources, so the grant form offers the real vocabulary instead of a free
    /// string. An operator grant is always recorded as <see cref="HabbiconSource.AdminGrant"/>
    /// whatever the form says — this list is for reading the ownership table, not for choosing.
    /// </summary>
    public object HabbiconSourceOptions()
    {
        List<object> items =
        [
            .. Enum.GetValues<HabbiconSource>()
                .Select(s => (object)new { name = s.ToString(), value = (int)s }),
        ];

        return new { count = items.Count, items };
    }

    private static object Describe(HabbiconEntity habbicon, IReadOnlyDictionary<int, int> owners) =>
        new
        {
            id = habbicon.Id,
            code = habbicon.Code,
            localizationKey = $"habbicon_{habbicon.Code}_name",
            collectionId = habbicon.HabbiconCollectionEntityId,
            sortOrder = habbicon.SortOrder,
            isCollectionReward = habbicon.IsCollectionReward,
            priceCredits = habbicon.PriceCredits,
            priceActivityPoints = habbicon.PriceActivityPoints,
            activityPointType = habbicon.ActivityPointType,
            enabled = habbicon.Enabled,
            availableFrom = habbicon.AvailableFrom,
            availableUntil = habbicon.AvailableUntil,
            owners = owners.GetValueOrDefault(habbicon.Id),
        };
}
