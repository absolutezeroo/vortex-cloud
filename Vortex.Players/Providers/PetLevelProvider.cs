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

public sealed class PetLevelProvider(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<IPetLevelProvider> logger
) : IPetLevelProvider
{
    private ImmutableDictionary<int, ImmutableArray<PetLevelEntry>> _byType = ImmutableDictionary<
        int,
        ImmutableArray<PetLevelEntry>
    >.Empty;
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<IPetLevelProvider> _logger = logger;

    public int GetLevelForExperience(int petType, int experience)
    {
        if (!_byType.TryGetValue(petType, out ImmutableArray<PetLevelEntry> levels))
        {
            return 1;
        }

        int level = 1;

        foreach (PetLevelEntry entry in levels)
        {
            if (experience >= entry.ExperienceRequired)
            {
                level = entry.Level;
            }
            else
            {
                break;
            }
        }

        return level;
    }

    public int GetExperienceForNextLevel(int petType, int currentLevel)
    {
        if (!_byType.TryGetValue(petType, out ImmutableArray<PetLevelEntry> levels))
        {
            return int.MaxValue;
        }

        PetLevelEntry? next = levels.FirstOrDefault(e => e.Level == currentLevel + 1);

        return next?.ExperienceRequired ?? int.MaxValue;
    }

    public int GetEnergyCapForLevel(int petType, int level)
    {
        PetLevelEntry? entry = GetEntry(petType, level);
        return entry?.EnergyCap ?? 100;
    }

    public int GetNutritionCapForLevel(int petType, int level)
    {
        PetLevelEntry? entry = GetEntry(petType, level);
        return entry?.NutritionCap ?? 100;
    }

    public PetLevelEntry? GetEntry(int petType, int level)
    {
        if (!_byType.TryGetValue(petType, out ImmutableArray<PetLevelEntry> levels))
        {
            return null;
        }

        return levels.FirstOrDefault(e => e.Level == level);
    }

    public int GetMaxLevel(int petType)
    {
        if (
            !_byType.TryGetValue(petType, out ImmutableArray<PetLevelEntry> levels)
            || levels.Length == 0
        )
        {
            return 1;
        }

        return levels[^1].Level;
    }

    public async Task ReloadAsync(CancellationToken ct)
    {
        VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            List<PetLevelEntity> entities = await dbCtx
                .PetLevels.AsNoTracking()
                .OrderBy(l => l.PetType)
                .ThenBy(l => l.Level)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            // GroupBy is order-preserving, so each bucket keeps the (already Level-ordered) sequence
            // the readers rely on.
            ImmutableDictionary<int, ImmutableArray<PetLevelEntry>> byType = entities
                .GroupBy(e => e.PetType)
                .ToImmutableDictionary(
                    g => g.Key,
                    g =>
                        g.Select(entity => new PetLevelEntry
                            {
                                PetType = entity.PetType,
                                Level = entity.Level,
                                ExperienceRequired = entity.ExperienceRequired,
                                EnergyCap = entity.EnergyCap,
                                NutritionCap = entity.NutritionCap,
                            })
                            .ToImmutableArray()
                );

            _byType = byType;

            _logger.LogInformation(
                "Loaded pet levels: {Count} entries across {Types} types",
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
