using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Dashboard.API.Infrastructure;
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

                // The client resolves a Habbicon's picture by id off its own spritesheet, so the
                // dashboard does too. Null when no pack is installed: the page then lists codes.
                HabbiconArtworkView? artwork = _habbiconArtwork.Read();

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
                            sprite = Sprite(artwork?.Collections, collection.Id),
                            habbicons = entries
                                .Select(h => Describe(h, ownersByHabbicon, artwork))
                                .Concat(
                                    reward is null
                                        ? []
                                        : new[] { Describe(reward, ownersByHabbicon, artwork) }
                                )
                                .ToList(),
                        }
                    );
                }

                return new
                {
                    count = items.Count,
                    items,
                    artwork = artwork is null
                        ? null
                        : new
                        {
                            spritesheetUrl = artwork.SpritesheetUrl,
                            collectionSpritesheetUrl = artwork.CollectionSpritesheetUrl,
                            frameSize = artwork.FrameSize,
                            collectionIconSize = artwork.CollectionIconSize,
                        },
                };
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

    /// <summary>
    /// Where an id sits on its sheet, or null when the pack does not carry it. Null rather than a
    /// zero offset on purpose: (0,0) is a real frame, so a missing entry that defaulted there would
    /// draw the first Habbicon under every id the pack forgot.
    /// </summary>
    private static object? Sprite(IReadOnlyDictionary<int, HabbiconFrame>? frames, int id) =>
        frames is not null && frames.TryGetValue(id, out HabbiconFrame frame)
            ? new { x = frame.X, y = frame.Y }
            : null;

    private static object Describe(
        HabbiconEntity habbicon,
        IReadOnlyDictionary<int, int> owners,
        HabbiconArtworkView? artwork
    ) =>
        new
        {
            sprite = Sprite(artwork?.Icons, habbicon.Id),
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
