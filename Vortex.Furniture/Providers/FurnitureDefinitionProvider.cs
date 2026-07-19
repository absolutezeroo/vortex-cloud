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

namespace Vortex.Furniture.Providers;

public sealed class FurnitureDefinitionProvider(
    IOptions<FurnitureConfig> config,
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<IFurnitureDefinitionProvider> logger
) : IFurnitureDefinitionProvider
{
    private readonly FurnitureConfig _config = config.Value;
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<IFurnitureDefinitionProvider> _logger = logger;

    private ImmutableDictionary<int, FurnitureDefinitionSnapshot> _definitionsById =
        ImmutableDictionary<int, FurnitureDefinitionSnapshot>.Empty;

    public FurnitureDefinitionSnapshot? TryGetDefinition(int id) =>
        _definitionsById.TryGetValue(id, out FurnitureDefinitionSnapshot? definition)
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
