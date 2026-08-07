using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Furniture.Configuration;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Furniture.Snapshots;
using Vortex.Primitives.Hosting;

namespace Vortex.Furniture.Providers;

public sealed class FurnitureDefinitionProvider(
    IOptions<FurnitureConfig> config,
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<IFurnitureDefinitionProvider> logger
) : IFurnitureDefinitionProvider, IReferenceDataProvider
{
    private readonly FurnitureConfig _config = config.Value;
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<IFurnitureDefinitionProvider> _logger = logger;

    // Stage 0: CatalogSnapshotProvider<T> and CatalogClubGiftProvider read furniture definitions
    // during their own reload (see stage 1), so this must finish first.
    public int LoadStage => 0;

    private ImmutableDictionary<int, FurnitureDefinitionSnapshot> _definitionsById =
        ImmutableDictionary<int, FurnitureDefinitionSnapshot>.Empty;

    // Classnames are not unique in this catalogue -- tile_stackmagic and roomdimmer each appear
    // twice -- so the index keeps the first of a duplicate rather than throwing the whole reload.
    private ImmutableDictionary<string, FurnitureDefinitionSnapshot> _definitionsByName =
        ImmutableDictionary<string, FurnitureDefinitionSnapshot>.Empty;

    public FurnitureDefinitionSnapshot? TryGetDefinition(int id) =>
        _definitionsById.TryGetValue(id, out FurnitureDefinitionSnapshot? definition)
            ? definition
            : null;

    public FurnitureDefinitionSnapshot? TryGetDefinitionByName(string name) =>
        _definitionsByName.TryGetValue(name, out FurnitureDefinitionSnapshot? definition)
            ? definition
            : null;

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            List<FurnitureDefinitionEntity> entities = await dbCtx
                .FurnitureDefinitions.AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            List<FurnitureDefinitionSnapshot> defs = entities
                .Select(x => new FurnitureDefinitionSnapshot
                {
                    Id = x.Id,
                    SpriteId = x.SpriteId,
                    Name = x.Name,
                    ProductType = x.ProductType,
                    FurniCategory = x.FurniCategory,
                    LogicName = x.Logic,
                    TotalStates = x.TotalStates,
                    Width = x.Width,
                    Length = x.Length,
                    StackHeight = Math.Round(Math.Max(_config.MinimumZValue, x.StackHeight), 2),
                    CanStack = x.CanStack,
                    CanWalk = x.CanWalk,
                    CanSit = x.CanSit,
                    CanLay = x.CanLay,
                    CanRecycle = x.CanRecycle,
                    CanTrade = x.CanTrade,
                    CanGroup = x.CanGroup,
                    CanSell = x.CanSell,
                    UsagePolicy = x.UsagePolicy,
                    ExtraData = x.ExtraData,
                    StuffDataType = x.StuffDataType,
                })
                .ToList();

            _definitionsById = defs.ToImmutableDictionary(p => p.Id);
            _definitionsByName = defs.DistinctBy(p => p.Name).ToImmutableDictionary(p => p.Name);

            _logger.LogInformation(
                "Loaded {TotalDefCount} furniture definitions",
                _definitionsById.Count
            );
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(false);
        }
    }
}
