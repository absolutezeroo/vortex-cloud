using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.MysteryBox;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// Read + analytics surface for the mystery box. The admin CRUD lives in
/// <c>DashboardOperationsService.MysteryBox.cs</c>; here we only read. Open/key analytics come from
/// the <c>audit_events</c> rows the mystery box audit handlers write — there is no dedicated
/// statistics table, and the key ledger (granted vs consumed) is exactly what tells an operator
/// whether keys are being farmed.
/// </summary>
internal sealed partial class DashboardApiService
{
    private const string MysteryBoxKeyGrantedAction = "mysterybox.key.granted";
    private const string MysteryBoxKeyConsumedAction = "mysterybox.key.consumed";
    private const string MysteryBoxOpenedAction = "mysterybox.opened";
    private const string MysteryBoxPrizeAwardedAction = "mysterybox.prize.awarded";
    private const string MysteryTrophyOpenedAction = "mysterytrophy.opened";

    /// <summary>The client logic name that makes a furniture definition a mystery box.</summary>
    private const string BoxLogicName = "furniture_mysterybox";

    /// <summary>The colours the client can render, so the admin picks one instead of typing a string
    /// that would silently make a box unpairable.</summary>
    public object MysteryBoxColorOptions() =>
        new { count = MysteryBoxColors.All.Length, items = MysteryBoxColors.All };

    /// <summary>
    /// The registered box colours and both prize pools, each joined to its furniture definition so
    /// the admin sees a name rather than a bare id. Definitions whose furniture is not actually
    /// running the mystery box logic are flagged: the row is valid but the client will never offer
    /// the dialog on that furni.
    /// </summary>
    public Task<object> MysteryBoxAsync(CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                // Which furniture can be a mystery box is decided by the client logic name, and a
                // box's colour by its own state — so the admin sees the definitions the client will
                // actually offer the dialog on, and how many boxes of each colour are in play.
                var definitions = await db
                    .FurnitureDefinitions.AsNoTracking()
                    .Where(d => d.Logic == BoxLogicName)
                    .OrderBy(d => d.Id)
                    .Select(d => new
                    {
                        d.Id,
                        d.Name,
                        d.SpriteId,
                        d.TotalStates,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var prizes = await db
                    .MysteryBoxPrizes.AsNoTracking()
                    .OrderBy(p => p.Pool)
                    .ThenByDescending(p => p.Weight)
                    .ThenBy(p => p.Id)
                    .Select(p => new
                    {
                        p.Id,
                        pool = p.Pool.ToString(),
                        p.Color,
                        productType = p.ProductType.ToString(),
                        furnitureDefinitionId = p.FurnitureDefinitionEntityId,
                        p.ExtraParam,
                        p.Weight,
                        p.Enabled,
                        furnitureName = db
                            .FurnitureDefinitions.Where(f => f.Id == p.FurnitureDefinitionEntityId)
                            .Select(f => f.Name)
                            .FirstOrDefault(),
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                // The odds an operator actually cares about are per pool and per colour, and they are
                // only meaningful relative to the entries that can be drawn together — so the share is
                // computed here rather than left to the page to guess.
                var pools = prizes
                    .Where(p => p.Enabled)
                    .GroupBy(p => new { p.pool, p.Color })
                    .Select(g => new
                    {
                        pool = g.Key.pool,
                        color = g.Key.Color,
                        totalWeight = g.Sum(p => p.Weight),
                        entries = g.Count(),
                    })
                    .OrderBy(g => g.pool)
                    .ThenBy(g => g.color)
                    .ToList();

                return new
                {
                    definitions = new { count = definitions.Count, items = definitions },
                    prizes = new { count = prizes.Count, items = prizes },
                    pools,
                    colors = MysteryBoxColors.All,
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
    /// Activity over a window (default 7 days): boxes and trophies opened, the key ledger, and the
    /// prizes players actually walked away with. A key granted but never consumed is still in
    /// circulation, which is the number that matters before adding more to the economy.
    /// </summary>
    public Task<object> MysteryBoxStatsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                int days = int.TryParse(query["days"], out int parsed)
                    ? Math.Clamp(parsed, 1, 90)
                    : 7;
                DateTime since = DateTime.UtcNow.AddDays(-days);

                string[] actions =
                [
                    MysteryBoxKeyGrantedAction,
                    MysteryBoxKeyConsumedAction,
                    MysteryBoxOpenedAction,
                    MysteryBoxPrizeAwardedAction,
                    MysteryTrophyOpenedAction,
                ];

                var counts = await db
                    .AuditEvents.AsNoTracking()
                    .Where(a => a.OccurredAt >= since && actions.Contains(a.Action))
                    .GroupBy(a => a.Action)
                    .Select(g => new { action = g.Key, count = g.Count() })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<string, int> byAction = counts.ToDictionary(
                    c => c.action,
                    c => c.count,
                    StringComparer.Ordinal
                );

                // Keys outstanding is a live count over the key table, not a windowed one: a key
                // granted last year and never spent is still in the economy today.
                int keysOutstanding = await db
                    .PlayerMysteryBoxKeys.AsNoTracking()
                    .CountAsync(k => k.ConsumedAt == null && k.DeletedAt == null, ct)
                    .ConfigureAwait(false);

                var keysByColor = await db
                    .PlayerMysteryBoxKeys.AsNoTracking()
                    .Where(k => k.DeletedAt == null)
                    .GroupBy(k => k.Color)
                    .Select(g => new
                    {
                        color = g.Key,
                        held = g.Count(k => k.ConsumedAt == null),
                        spent = g.Count(k => k.ConsumedAt != null),
                    })
                    .OrderBy(g => g.color)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                int boxesPlaced = await db
                    .Furnitures.AsNoTracking()
                    .CountAsync(
                        f =>
                            f.DeletedAt == null
                            && db.FurnitureDefinitions.Any(d =>
                                d.Id == f.FurnitureDefinitionEntityId && d.Logic == BoxLogicName
                            ),
                        ct
                    )
                    .ConfigureAwait(false);

                return new
                {
                    days,
                    since,
                    boxesOpened = byAction.GetValueOrDefault(MysteryBoxOpenedAction),
                    trophiesOpened = byAction.GetValueOrDefault(MysteryTrophyOpenedAction),
                    prizesAwarded = byAction.GetValueOrDefault(MysteryBoxPrizeAwardedAction),
                    keysGranted = byAction.GetValueOrDefault(MysteryBoxKeyGrantedAction),
                    keysConsumed = byAction.GetValueOrDefault(MysteryBoxKeyConsumedAction),
                    keysOutstanding,
                    keysByColor,
                    boxesInCirculation = boxesPlaced,
                };
            },
            ct
        );
}
