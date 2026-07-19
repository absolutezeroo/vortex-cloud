using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Pets;
using Vortex.Primitives.Pets.Providers;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.Players.Providers;

public sealed class PetPaletteProvider(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    ILogger<IPetPaletteProvider> logger
) : IPetPaletteProvider
{
    private ImmutableDictionary<int, ImmutableArray<PetPaletteEntry>> _byType = ImmutableDictionary<
        int,
        ImmutableArray<PetPaletteEntry>
    >.Empty;
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<IPetPaletteProvider> _logger = logger;

    public IReadOnlyList<PetPaletteEntry> GetPalettesForType(int petType)
    {
        if (_byType.TryGetValue(petType, out ImmutableArray<PetPaletteEntry> entries))
        {
            return entries;
        }

        return [];
    }

    public async Task ReloadAsync(CancellationToken ct)
    {
        TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            List<PetPaletteEntity> entities = await dbCtx
                .PetPalettes.AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            ImmutableDictionary<int, ImmutableArray<PetPaletteEntry>> byType = entities
                .GroupBy(e => e.PetType)
                .ToImmutableDictionary(
                    g => g.Key,
                    g =>
                        g.Select(entity => new PetPaletteEntry
                            {
                                PetType = entity.PetType,
                                BreedIndex = entity.BreedIndex,
                                Color = entity.Color,
                                Sellable = entity.Sellable,
                                Rare = entity.Rare,
                            })
                            .ToImmutableArray()
                );

            _byType = byType;

            _logger.LogInformation(
                "Loaded pet palettes: {Count} entries across {Types} types",
                entities.Count,
                byType.Count
            );
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(false);
        }
    }
}
