using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Dashboard.API.Api.Progression.Contracts;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Database.Context;
using Vortex.Database.Entities.Habbicons;
using Vortex.Primitives.Habbicons;

namespace Vortex.Dashboard.API.Api.Progression;

/// <summary>
/// Read surface for Habbicon content and ownership. The CRUD lives in
/// <see cref="Operations.RewardOperations"/>; here we only read.
/// </summary>
/// <remarks>
/// Counts are aggregated straight from <c>player_habbicons</c> — ownership is the only source of
/// truth for whether a set is complete, and there is no cached completion column to read instead.
/// </remarks>
internal sealed class HabbiconReads(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    HabbiconArtwork habbiconArtwork
) : DashboardReads(dbContextFactory)
{
    private readonly HabbiconArtwork _habbiconArtwork = habbiconArtwork;

    /// <summary>
    /// Every collection with its members and how the hotel is doing on it: how many players own at
    /// least one entry, and how many own the lot.
    /// </summary>
    public Task<HabbiconCollectionList> HabbiconCollectionsAsync(
        NameValueCollection query,
        CancellationToken ct
    ) =>
        QueryAsync<HabbiconCollectionList>(
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

                List<HabbiconCollectionRow> items = [];

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
                        new HabbiconCollectionRow(
                            collection.Id,
                            collection.Code,
                            // The client renders habbicon_collection_<code>_name; showing the key
                            // beside the code saves an operator guessing what to add to the texts.
                            $"habbicon_collection_{collection.Code}_name",
                            collection.SortOrder,
                            collection.Enabled,
                            collection.Hidden,
                            collection.AvailableFrom,
                            collection.AvailableUntil,
                            collection.PriceCredits,
                            collection.PriceActivityPoints,
                            collection.ActivityPointType,
                            collection.CampaignCode,
                            entries.Count,
                            reward?.Id ?? 0,
                            reward?.Code ?? string.Empty,
                            completedBy,
                            Sprite(artwork?.Collections, collection.Id),
                            entries
                                .Select(h => Describe(h, ownersByHabbicon, artwork))
                                .Concat(
                                    reward is null
                                        ? []
                                        : new[] { Describe(reward, ownersByHabbicon, artwork) }
                                )
                                .ToList()
                        )
                    );
                }

                return new HabbiconCollectionList(
                    items.Count,
                    items,
                    artwork is null
                        ? null
                        : new HabbiconSheets(
                            artwork.SpritesheetUrl,
                            artwork.CollectionSpritesheetUrl,
                            artwork.FrameSize,
                            artwork.CollectionIconSize
                        )
                );
            },
            ct
        );

    /// <summary>
    /// One player's Habbicons, with where each came from and when they last used it. What an
    /// operator opens when somebody asks why they do or do not have something.
    /// </summary>
    public Task<PlayerHabbicons> PlayerHabbiconsAsync(int playerId, CancellationToken ct) =>
        QueryAsync<PlayerHabbicons>(
            async db =>
            {
                List<PlayerHabbiconEntity> rows = await db
                    .PlayerHabbicons.AsNoTracking()
                    .Include(p => p.Habbicon)
                    .Where(p => p.PlayerEntityId == playerId && p.DeletedAt == null)
                    .OrderByDescending(p => p.AcquiredAt)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                return new PlayerHabbicons(
                    playerId,
                    rows.Count,
                    rows.Select(r => new PlayerHabbiconRow(
                            r.HabbiconEntityId,
                            r.Habbicon?.Code ?? string.Empty,
                            r.Habbicon?.HabbiconCollectionEntityId ?? 0,
                            r.State.ToString(),
                            r.Source.ToString(),
                            r.AcquiredAt,
                            r.LastUsedAt
                        ))
                        .ToList()
                );
            },
            ct
        );

    /// <summary>
    /// The acquisition sources, so the grant form offers the real vocabulary instead of a free
    /// string. An operator grant is always recorded as <see cref="HabbiconSource.AdminGrant"/>
    /// whatever the form says — this list is for reading the ownership table, not for choosing.
    /// </summary>
    public HabbiconSourceOptions HabbiconSourceOptions()
    {
        List<HabbiconSourceOption> items =
        [
            .. Enum.GetValues<HabbiconSource>()
                .Select(s => new HabbiconSourceOption(s.ToString(), (int)s)),
        ];

        return new HabbiconSourceOptions(items.Count, items);
    }

    /// <summary>
    /// Where an id sits on its sheet, or null when the pack does not carry it. Null rather than a
    /// zero offset on purpose: (0,0) is a real frame, so a missing entry that defaulted there would
    /// draw the first Habbicon under every id the pack forgot.
    /// </summary>
    private static HabbiconSprite? Sprite(
        IReadOnlyDictionary<int, HabbiconFrame>? frames,
        int id
    ) =>
        frames is not null && frames.TryGetValue(id, out HabbiconFrame frame)
            ? new HabbiconSprite(frame.X, frame.Y)
            : null;

    private static HabbiconRow Describe(
        HabbiconEntity habbicon,
        IReadOnlyDictionary<int, int> owners,
        HabbiconArtworkView? artwork
    ) =>
        new(
            Sprite(artwork?.Icons, habbicon.Id),
            habbicon.Id,
            habbicon.Code,
            $"habbicon_{habbicon.Code}_name",
            habbicon.HabbiconCollectionEntityId,
            habbicon.SortOrder,
            habbicon.IsCollectionReward,
            habbicon.PriceCredits,
            habbicon.PriceActivityPoints,
            habbicon.ActivityPointType,
            habbicon.Enabled,
            habbicon.AvailableFrom,
            habbicon.AvailableUntil,
            owners.GetValueOrDefault(habbicon.Id)
        );
}
