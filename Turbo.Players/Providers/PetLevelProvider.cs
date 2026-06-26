using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Turbo.Database.Context;
using Turbo.Database.Entities.Pets;
using Turbo.Primitives.Pets.Providers;
using Turbo.Primitives.Pets.Snapshots;

namespace Turbo.Players.Providers;

public sealed class PetLevelProvider(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    ILogger<IPetLevelProvider> logger
) : IPetLevelProvider
{
    private readonly Dictionary<int, List<PetLevelEntry>> _byType = [];
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<IPetLevelProvider> _logger = logger;

    public int GetLevelForExperience(int petType, int experience)
    {
        if (!_byType.TryGetValue(petType, out List<PetLevelEntry>? levels))
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
        if (!_byType.TryGetValue(petType, out List<PetLevelEntry>? levels))
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
        if (!_byType.TryGetValue(petType, out List<PetLevelEntry>? levels))
        {
            return null;
        }

        return levels.FirstOrDefault(e => e.Level == level);
    }

    public int GetMaxLevel(int petType)
    {
        if (!_byType.TryGetValue(petType, out List<PetLevelEntry>? levels) || levels.Count == 0)
        {
            return 1;
        }

        return levels[^1].Level;
    }

    public async Task ReloadAsync(CancellationToken ct)
    {
        _byType.Clear();

        TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            List<PetLevelEntity> entities = await dbCtx
                .PetLevels.AsNoTracking()
                .OrderBy(l => l.PetType)
                .ThenBy(l => l.Level)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (PetLevelEntity entity in entities)
            {
                if (!_byType.TryGetValue(entity.PetType, out List<PetLevelEntry>? bucket))
                {
                    bucket = [];
                    _byType[entity.PetType] = bucket;
                }

                bucket.Add(
                    new PetLevelEntry
                    {
                        PetType = entity.PetType,
                        Level = entity.Level,
                        ExperienceRequired = entity.ExperienceRequired,
                        EnergyCap = entity.EnergyCap,
                        NutritionCap = entity.NutritionCap,
                    }
                );
            }

            _logger.LogInformation(
                "Loaded pet levels: {Count} entries across {Types} types",
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
