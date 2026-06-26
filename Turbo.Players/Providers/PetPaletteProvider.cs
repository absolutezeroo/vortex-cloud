using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Turbo.Database.Context;
using Turbo.Database.Entities.Pets;
using Turbo.Primitives.Pets.Providers;
using Turbo.Primitives.Pets.Snapshots;

namespace Turbo.Players.Providers;

public sealed class PetPaletteProvider(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    ILogger<IPetPaletteProvider> logger
) : IPetPaletteProvider
{
    private readonly Dictionary<int, List<PetPaletteEntry>> _byType = [];
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<IPetPaletteProvider> _logger = logger;

    public IReadOnlyList<PetPaletteEntry> GetPalettesForType(int petType)
    {
        if (_byType.TryGetValue(petType, out List<PetPaletteEntry>? entries))
        {
            return entries;
        }

        return [];
    }

    public async Task ReloadAsync(CancellationToken ct)
    {
        _byType.Clear();

        TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            List<PetPaletteEntity> entities = await dbCtx
                .PetPalettes.AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (PetPaletteEntity entity in entities)
            {
                if (!_byType.TryGetValue(entity.PetType, out List<PetPaletteEntry>? bucket))
                {
                    bucket = [];
                    _byType[entity.PetType] = bucket;
                }

                bucket.Add(
                    new PetPaletteEntry
                    {
                        PetType = entity.PetType,
                        BreedIndex = entity.BreedIndex,
                        Color = entity.Color,
                        Sellable = entity.Sellable,
                        Rare = entity.Rare,
                    }
                );
            }

            _logger.LogInformation(
                "Loaded pet palettes: {Count} entries across {Types} types",
                entities.Count,
                _byType.Count
            );
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(false);
        }
    }
}
