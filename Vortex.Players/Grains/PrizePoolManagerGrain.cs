using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Prizes;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Prizes;
using Vortex.Primitives.Prizes.Grains;
using Vortex.Primitives.Prizes.Snapshots;

namespace Vortex.Players.Grains;

/// <summary>
/// Caches every prize pool for the lifetime of the kept-alive singleton. The pools are read on each
/// draw, so re-querying them per open would put a table scan on the hot path for data that changes
/// only when an admin edits it.
/// </summary>
[KeepAlive]
internal sealed class PrizePoolManagerGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<PrizePoolManagerGrain> logger
) : Grain, IPrizePoolManagerGrain
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<PrizePoolManagerGrain> _logger = logger;

    /// <summary>Prize types the client's reward window can actually draw; anything else would show a
    /// blank dialog, so such rows are dropped at load time with a warning rather than silently
    /// handed out.</summary>
    private static readonly ProductType[] DrawableProductTypes =
    [
        ProductType.Floor,
        ProductType.Wall,
        ProductType.Effect,
        ProductType.HabboClub,
    ];

    private ImmutableArray<PrizeEntrySnapshot> _entries = [];
    private bool _loaded;

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await LoadAsync(ct).ConfigureAwait(true);
        await base.OnActivateAsync(ct).ConfigureAwait(true);
    }

    public async Task<PrizeEntrySnapshot?> PickAsync(
        string poolCode,
        string variant,
        CancellationToken ct
    )
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(true);

        PrizeEntrySnapshot? entry = PrizePicker.Pick(_entries, poolCode, variant);

        if (entry is null)
        {
            _logger.LogWarning(
                "Prize pool '{PoolCode}' has no enabled entry for variant '{Variant}'; nothing can be awarded.",
                poolCode,
                variant
            );
        }

        return entry;
    }

    public Task ReloadAsync(CancellationToken ct) => LoadAsync(ct);

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (!_loaded)
        {
            await LoadAsync(ct).ConfigureAwait(true);
        }
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<PrizePoolEntity> pools = await dbCtx
                .PrizePools.AsNoTracking()
                .Where(p => p.Enabled && p.DeletedAt == null)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            Dictionary<int, PrizePoolEntity> poolsById = pools.ToDictionary(p => p.Id);

            List<PrizePoolEntryEntity> entryRows = await dbCtx
                .PrizePoolEntries.AsNoTracking()
                .Where(e => e.Enabled && e.Weight > 0 && e.DeletedAt == null)
                .OrderBy(e => e.Id)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            List<PrizeEntrySnapshot> entries = [];

            foreach (PrizePoolEntryEntity row in entryRows)
            {
                // An entry whose pool is disabled or deleted is not an error worth logging on every
                // reload: disabling a pool is exactly how an operator retires a seasonal event.
                if (!poolsById.TryGetValue(row.PrizePoolEntityId, out PrizePoolEntity? pool))
                {
                    continue;
                }

                if (!DrawableProductTypes.Contains(row.ProductType))
                {
                    _logger.LogWarning(
                        "Prize entry {Id} has product type {ProductType}, which the reward window cannot draw; skipping it.",
                        row.Id,
                        row.ProductType
                    );

                    continue;
                }

                entries.Add(
                    new PrizeEntrySnapshot
                    {
                        Id = row.Id,
                        PoolCode = pool.Code,
                        Variant = PrizeVariants.NormalizeForSet(
                            row.Variant,
                            PrizeVariants.ParseSet(pool.Variants)
                        ),
                        ProductType = row.ProductType,
                        FurnitureDefinitionId = row.FurnitureDefinitionEntityId,
                        ExtraParam = row.ExtraParam,
                        Weight = row.Weight,
                    }
                );
            }

            _entries = [.. entries];
            _loaded = true;

            _logger.LogInformation(
                "Loaded {PoolCount} prize pools and {EntryCount} entries into cache.",
                pools.Count,
                _entries.Length
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load prize pool reference data.");
        }
    }
}
