using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Prizes;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// Read surface for the prize pools. The admin CRUD lives in
/// <c>DashboardOperationsService.Prizes.cs</c>; here we only read.
///
/// The share percentages are computed server-side rather than left to the page: an operator typing
/// weights 6/3/1 has written 60/30/10 and needs to see that, and the competing set is not obvious —
/// an entry with no variant competes for every variant of its pool, so its denominator differs from
/// a variant-locked one.
/// </summary>
internal sealed partial class DashboardApiService
{
    private const string PrizeAwardedAction = "prize.awarded";

    public Task<object> PrizePoolsAsync(CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                var pools = await db
                    .PrizePools.AsNoTracking()
                    .OrderBy(p => p.Code)
                    .Select(p => new
                    {
                        p.Id,
                        p.Code,
                        p.Name,
                        p.Variants,
                        p.Notes,
                        p.Enabled,
                        isBuiltIn = p.Code == PrizePoolCodes.MysteryBox
                            || p.Code == PrizePoolCodes.MysteryTrophy,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var entries = await db
                    .PrizePoolEntries.AsNoTracking()
                    .Where(e => e.PrizePoolEntity != null)
                    .OrderByDescending(e => e.Weight)
                    .ThenBy(e => e.Id)
                    .Select(e => new
                    {
                        e.Id,
                        poolId = e.PrizePoolEntityId,
                        pool = e.PrizePoolEntity!.Code,
                        e.Variant,
                        productType = e.ProductType.ToString(),
                        furnitureDefinitionId = e.FurnitureDefinitionEntityId,
                        e.ExtraParam,
                        e.Weight,
                        e.Enabled,
                        furnitureName = db
                            .FurnitureDefinitions.Where(f => f.Id == e.FurnitureDefinitionEntityId)
                            .Select(f => f.Name)
                            .FirstOrDefault(),
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var entryRows = entries
                    .Select(e => new
                    {
                        e.Id,
                        e.poolId,
                        e.pool,
                        e.Variant,
                        e.productType,
                        e.furnitureDefinitionId,
                        e.ExtraParam,
                        e.Weight,
                        e.Enabled,
                        e.furnitureName,
                        // An operator recognises a sofa, not definition id 4312.
                        furnitureIconUrl = e.furnitureName is null
                            ? null
                            : BuildFurniIconUrl(e.furnitureName),
                    })
                    .ToList();

                // The competing set for an entry is: same pool, and either variantless (competes
                // everywhere) or locked to the same variant. Computing it per (pool, variant) group
                // is what makes the number an operator reads match what the picker actually does.
                var totals = entries
                    .Where(e => e.Enabled)
                    .GroupBy(e => new { e.poolId, e.Variant })
                    .Select(g => new
                    {
                        g.Key.poolId,
                        variant = g.Key.Variant,
                        totalWeight = g.Sum(e => e.Weight),
                        entries = g.Count(),
                    })
                    .OrderBy(g => g.poolId)
                    .ThenBy(g => g.variant)
                    .ToList();

                var bindings = await db
                    .PrizePoolBindings.AsNoTracking()
                    .Where(b => b.PrizePoolEntity != null)
                    .OrderBy(b => b.FurnitureDefinitionEntityId)
                    .Select(b => new
                    {
                        b.Id,
                        furnitureDefinitionId = b.FurnitureDefinitionEntityId,
                        pool = b.PrizePoolEntity!.Code,
                        b.HitsRequired,
                        b.Enabled,
                        furnitureName = db
                            .FurnitureDefinitions.Where(f => f.Id == b.FurnitureDefinitionEntityId)
                            .Select(f => f.Name)
                            .FirstOrDefault(),
                        furnitureLogic = db
                            .FurnitureDefinitions.Where(f => f.Id == b.FurnitureDefinitionEntityId)
                            .Select(f => f.Logic)
                            .FirstOrDefault(),
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var bindingRows = bindings
                    .Select(b => new
                    {
                        b.Id,
                        b.furnitureDefinitionId,
                        b.pool,
                        b.HitsRequired,
                        b.Enabled,
                        b.furnitureName,
                        b.furnitureLogic,
                        furnitureIconUrl = b.furnitureName is null
                            ? null
                            : BuildFurniIconUrl(b.furnitureName),
                    })
                    .ToList();

                return new
                {
                    pools = new { count = pools.Count, items = pools },
                    entries = new { count = entryRows.Count, items = entryRows },
                    totals,
                    bindings = new { count = bindingRows.Count, items = bindingRows },
                    productTypes = new[]
                    {
                        ProductType.Floor.ToString(),
                        ProductType.Wall.ToString(),
                        ProductType.Effect.ToString(),
                        ProductType.HabboClub.ToString(),
                    },
                };
            },
            ct
        );

    /// <summary>
    /// What each pool really paid out over the window, from the <c>prize.awarded</c> audit rows the
    /// grant grain writes. This is the half a weights table cannot tell you: a pool can be tuned
    /// correctly and still pay out nothing because the furniture bound to it is unreachable.
    /// </summary>
    public Task<object> PrizePoolStatsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                int days = int.TryParse(query["days"], out int parsed)
                    ? Math.Clamp(parsed, 1, 90)
                    : 7;
                DateTime since = DateTime.UtcNow.AddDays(-days);

                List<string?> payloads = await db
                    .AuditEvents.AsNoTracking()
                    .Where(a => a.OccurredAt >= since && a.Action == PrizeAwardedAction)
                    .Select(a => a.Data)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<string, Dictionary<int, int>> drawsByPool = new(StringComparer.Ordinal);
                Dictionary<string, Dictionary<string, int>> drawsBySource = new(
                    StringComparer.Ordinal
                );

                foreach (string? payload in payloads)
                {
                    if (string.IsNullOrWhiteSpace(payload))
                    {
                        continue;
                    }

                    string? pool;
                    int entryId;
                    string source;

                    try
                    {
                        using JsonDocument document = JsonDocument.Parse(payload);
                        JsonElement root = document.RootElement;

                        pool = root.TryGetProperty("pool", out JsonElement p)
                            ? p.GetString()
                            : null;
                        entryId = root.TryGetProperty("entryId", out JsonElement e)
                            ? e.GetInt32()
                            : 0;
                        source = root.TryGetProperty("source", out JsonElement s)
                            ? s.GetString() ?? string.Empty
                            : string.Empty;
                    }
                    catch (JsonException)
                    {
                        // A malformed audit payload is a bug elsewhere; dropping the row here keeps
                        // the page readable instead of failing the whole window.
                        continue;
                    }

                    if (string.IsNullOrEmpty(pool))
                    {
                        continue;
                    }

                    Dictionary<int, int> byEntry = drawsByPool.TryGetValue(
                        pool,
                        out Dictionary<int, int>? existing
                    )
                        ? existing
                        : drawsByPool[pool] = [];

                    byEntry[entryId] = byEntry.GetValueOrDefault(entryId) + 1;

                    Dictionary<string, int> bySource = drawsBySource.TryGetValue(
                        pool,
                        out Dictionary<string, int>? existingSources
                    )
                        ? existingSources
                        : drawsBySource[pool] = new Dictionary<string, int>(StringComparer.Ordinal);

                    bySource[source] = bySource.GetValueOrDefault(source) + 1;
                }

                var pools = drawsByPool
                    .Select(p => new
                    {
                        pool = p.Key,
                        draws = p.Value.Values.Sum(),
                        entries = p
                            .Value.OrderByDescending(e => e.Value)
                            .Select(e => new { entryId = e.Key, draws = e.Value })
                            .ToList(),
                        sources = drawsBySource.TryGetValue(
                            p.Key,
                            out Dictionary<string, int>? sources
                        )
                            ? sources
                                .OrderByDescending(s => s.Value)
                                .Select(s => new { source = s.Key, draws = s.Value })
                                .ToList()
                            : [],
                    })
                    .OrderByDescending(p => p.draws)
                    .ToList();

                return new
                {
                    days,
                    totalDraws = payloads.Count,
                    pools,
                };
            },
            ct
        );
}
