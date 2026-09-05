using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Habbicons;
using Vortex.Primitives.Habbicons;
using Vortex.Primitives.Habbicons.Snapshots;
using Vortex.Primitives.Hosting;

namespace Vortex.Habbicons;

/// <summary>
/// The Habbicon definitions and collections, held in memory for the life of the process and
/// reloaded when an operator changes them.
/// </summary>
/// <remarks>
/// A reference cache rather than a grain because every read is a read: the hub, a purchase check and
/// a use check all need the same few hundred rows, and none of them mutates any. Making it a grain
/// would put a network hop in front of a dictionary lookup for no gain at all.
/// </remarks>
internal sealed class HabbiconCatalog(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    ILogger<HabbiconCatalog> logger
) : IHabbiconCatalog, IReferenceDataProvider
{
    private ImmutableArray<HabbiconCollectionSnapshot> _collections = [];
    private FrozenLookups _lookups = FrozenLookups.Empty;

    public int LoadStage => 0;

    public ImmutableArray<HabbiconCollectionSnapshot> Collections => _collections;

    public bool TryGetHabbicon(
        int habbiconId,
        [NotNullWhen(true)] out HabbiconDefinitionSnapshot? definition
    ) => _lookups.ByHabbiconId.TryGetValue(habbiconId, out definition);

    public bool TryGetCollection(
        int collectionId,
        [NotNullWhen(true)] out HabbiconCollectionSnapshot? collection
    ) => _lookups.ByCollectionId.TryGetValue(collectionId, out collection);

    public bool TryGetCollectionOf(
        int habbiconId,
        [NotNullWhen(true)] out HabbiconCollectionSnapshot? collection
    ) => _lookups.CollectionOfHabbicon.TryGetValue(habbiconId, out collection);

    public async Task ReloadAsync(CancellationToken ct)
    {
        try
        {
            await using VortexDbContext db = await dbContextFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            List<HabbiconCollectionEntity> collectionRows = await db
                .HabbiconCollections.AsNoTracking()
                .Where(c => c.DeletedAt == null)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            List<HabbiconEntity> habbiconRows = await db
                .Habbicons.AsNoTracking()
                .Where(h => h.DeletedAt == null)
                .OrderBy(h => h.SortOrder)
                .ThenBy(h => h.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            ILookup<int, HabbiconEntity> byCollection = habbiconRows.ToLookup(h =>
                h.HabbiconCollectionEntityId
            );

            ImmutableArray<HabbiconCollectionSnapshot> collections =
            [
                .. collectionRows.Select(c => BuildCollection(c, byCollection[c.Id])),
            ];

            _collections = collections;
            _lookups = FrozenLookups.Build(collections);

            logger.LogInformation(
                "Loaded {CollectionCount} Habbicon collection(s) and {HabbiconCount} Habbicon(s).",
                collections.Length,
                _lookups.ByHabbiconId.Count
            );
        }
        catch (Exception ex)
        {
            // A failed reload keeps the previous catalog. The hotel goes on serving what it had,
            // which beats every collection vanishing from every album because one query timed out.
            logger.LogError(ex, "Failed to load the Habbicon catalog; keeping the previous one.");
        }
    }

    private static HabbiconCollectionSnapshot BuildCollection(
        HabbiconCollectionEntity row,
        IEnumerable<HabbiconEntity> members
    )
    {
        List<HabbiconEntity> all = [.. members];

        return new HabbiconCollectionSnapshot
        {
            CollectionId = row.Id,
            Code = row.Code,
            SortOrder = row.SortOrder,
            Enabled = row.Enabled,
            Hidden = row.Hidden,
            AvailableFromUtc = row.AvailableFrom,
            AvailableUntilUtc = row.AvailableUntil,
            PriceCredits = row.PriceCredits,
            PriceActivityPoints = row.PriceActivityPoints,
            ActivityPointType = row.ActivityPointType,
            CampaignCode = row.CampaignCode,
            Entries = [.. all.Where(h => !h.IsCollectionReward).Select(ToDefinition)],
            // First rather than single: a second reward row is a content error the validator
            // reports, and refusing to load the whole catalog over it would be a worse failure.
            RewardHabbicon = all.Where(h => h.IsCollectionReward)
                .Select(ToDefinition)
                .FirstOrDefault(),
        };
    }

    private static HabbiconDefinitionSnapshot ToDefinition(HabbiconEntity row) =>
        new()
        {
            HabbiconId = row.Id,
            Code = row.Code,
            CollectionId = row.HabbiconCollectionEntityId,
            SortOrder = row.SortOrder,
            IsCollectionReward = row.IsCollectionReward,
            PriceCredits = row.PriceCredits,
            PriceActivityPoints = row.PriceActivityPoints,
            ActivityPointType = row.ActivityPointType,
            Enabled = row.Enabled,
            AvailableFromUtc = row.AvailableFrom,
            AvailableUntilUtc = row.AvailableUntil,
        };

    /// <summary>
    /// The three lookups every caller wants, rebuilt as one unit so a reload can never be observed
    /// half-applied: the field is swapped once, and a reader either sees all of the old catalog or
    /// all of the new one.
    /// </summary>
    private sealed record FrozenLookups(
        IReadOnlyDictionary<int, HabbiconDefinitionSnapshot> ByHabbiconId,
        IReadOnlyDictionary<int, HabbiconCollectionSnapshot> ByCollectionId,
        IReadOnlyDictionary<int, HabbiconCollectionSnapshot> CollectionOfHabbicon
    )
    {
        public static FrozenLookups Empty { get; } =
            new(
                new Dictionary<int, HabbiconDefinitionSnapshot>(),
                new Dictionary<int, HabbiconCollectionSnapshot>(),
                new Dictionary<int, HabbiconCollectionSnapshot>()
            );

        public static FrozenLookups Build(ImmutableArray<HabbiconCollectionSnapshot> collections)
        {
            Dictionary<int, HabbiconDefinitionSnapshot> byHabbicon = [];
            Dictionary<int, HabbiconCollectionSnapshot> byCollection = [];
            Dictionary<int, HabbiconCollectionSnapshot> collectionOf = [];

            foreach (HabbiconCollectionSnapshot collection in collections)
            {
                byCollection[collection.CollectionId] = collection;

                foreach (HabbiconDefinitionSnapshot entry in collection.Entries)
                {
                    byHabbicon[entry.HabbiconId] = entry;
                    collectionOf[entry.HabbiconId] = collection;
                }

                if (collection.RewardHabbicon is not null)
                {
                    byHabbicon[collection.RewardHabbicon.HabbiconId] = collection.RewardHabbicon;
                    collectionOf[collection.RewardHabbicon.HabbiconId] = collection;
                }
            }

            return new FrozenLookups(byHabbicon, byCollection, collectionOf);
        }
    }
}
