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
using Vortex.Database.Entities.MysteryBox;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.MysteryBox;
using Vortex.Primitives.MysteryBox.Grains;
using Vortex.Primitives.MysteryBox.Snapshots;

namespace Vortex.Players.Grains;

/// <summary>
/// Caches the mystery box reference data for the lifetime of the kept-alive singleton. Box colours
/// are read on every room click and every tracker push, and the prize pools on every open, so
/// re-querying them per event would put two table scans on the hot path for data that changes only
/// when an admin edits it.
/// </summary>
[KeepAlive]
internal sealed class MysteryBoxManagerGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<MysteryBoxManagerGrain> logger
) : Grain, IMysteryBoxManagerGrain
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<MysteryBoxManagerGrain> _logger = logger;

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

    /// <summary>The client only offers the open dialog on furniture carrying this logic name
    /// (RoomObjectLogicEnum), so it is also what makes a definition a mystery box server-side.</summary>
    private const string BoxLogicName = "furniture_mysterybox";

    private ImmutableArray<int> _boxDefinitionIds = [];
    private ImmutableArray<MysteryBoxPrizeSnapshot> _prizes = [];
    private HashSet<int> _boxDefinitionIdSet = [];
    private bool _loaded;

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await LoadAsync(ct).ConfigureAwait(true);
        await base.OnActivateAsync(ct).ConfigureAwait(true);
    }

    public async Task<ImmutableArray<int>> GetBoxDefinitionIdsAsync(CancellationToken ct)
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(true);

        return _boxDefinitionIds;
    }

    public async Task<bool> IsBoxDefinitionAsync(int definitionId, CancellationToken ct)
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(true);

        return _boxDefinitionIdSet.Contains(definitionId);
    }

    public async Task<MysteryBoxPrizeSnapshot?> PickPrizeAsync(
        MysteryBoxPrizePool pool,
        string color,
        CancellationToken ct
    )
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(true);

        MysteryBoxPrizeSnapshot? prize = MysteryBoxPrizePicker.Pick(_prizes, pool, color);

        if (prize is null)
        {
            _logger.LogWarning(
                "Mystery box prize pool {Pool} has no enabled entry for colour '{Color}'; nothing can be awarded.",
                pool,
                color
            );
        }

        return prize;
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

            List<int> boxDefinitionIds = await dbCtx
                .FurnitureDefinitions.AsNoTracking()
                .Where(d => d.Logic == BoxLogicName)
                .Select(d => d.Id)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            List<MysteryBoxPrizeEntity> prizeRows = await dbCtx
                .MysteryBoxPrizes.AsNoTracking()
                .Where(p => p.Enabled && p.Weight > 0 && p.DeletedAt == null)
                .OrderBy(p => p.Id)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            List<MysteryBoxPrizeSnapshot> prizes = [];

            foreach (MysteryBoxPrizeEntity row in prizeRows)
            {
                if (!DrawableProductTypes.Contains(row.ProductType))
                {
                    _logger.LogWarning(
                        "Mystery box prize {Id} has product type {ProductType}, which the reward window cannot draw; skipping it.",
                        row.Id,
                        row.ProductType
                    );

                    continue;
                }

                prizes.Add(
                    new MysteryBoxPrizeSnapshot
                    {
                        Id = row.Id,
                        Pool = row.Pool,
                        // An unrenderable colour on a prize would make it undrawable forever; treat
                        // it as "any colour" instead, matching the empty-string default.
                        Color = MysteryBoxColors.Normalize(row.Color),
                        ProductType = row.ProductType,
                        FurnitureDefinitionId = row.FurnitureDefinitionEntityId,
                        ExtraParam = row.ExtraParam,
                        Weight = row.Weight,
                    }
                );
            }

            _boxDefinitionIds = [.. boxDefinitionIds];
            _boxDefinitionIdSet = [.. boxDefinitionIds];
            _prizes = [.. prizes];
            _loaded = true;

            _logger.LogInformation(
                "Loaded {BoxCount} mystery box furniture definitions and {PrizeCount} prizes into cache.",
                _boxDefinitionIds.Length,
                _prizes.Length
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load mystery box reference data.");
        }
    }
}
