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
using Vortex.Primitives.MysteryBox.Grains;

namespace Vortex.Players.Grains;

/// <summary>
/// Caches which furniture definitions run the mystery box logic, for the lifetime of the kept-alive
/// singleton. Box definitions are read on every room click and every tracker push, so re-querying
/// them per event would put a table scan on the hot path for data that changes only when an admin
/// edits it.
///
/// The prizes themselves are not here: they live in <see cref="Vortex.Primitives.Prizes.Grains.IPrizePoolManagerGrain"/>
/// under the pool code <c>mystery-box</c>, shared with every other furniture that hands out a
/// weighted reward.
/// </summary>
[KeepAlive]
internal sealed class MysteryBoxManagerGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<MysteryBoxManagerGrain> logger
) : Grain, IMysteryBoxManagerGrain
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<MysteryBoxManagerGrain> _logger = logger;

    /// <summary>The client only offers the open dialog on furniture carrying this logic name
    /// (RoomObjectLogicEnum), so it is also what makes a definition a mystery box server-side.</summary>
    private const string BoxLogicName = "furniture_mysterybox";

    private ImmutableArray<int> _boxDefinitionIds = [];
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

            _boxDefinitionIds = [.. boxDefinitionIds];
            _boxDefinitionIdSet = [.. boxDefinitionIds];
            _loaded = true;

            _logger.LogInformation(
                "Loaded {BoxCount} mystery box furniture definitions into cache.",
                _boxDefinitionIds.Length
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load mystery box reference data.");
        }
    }
}
