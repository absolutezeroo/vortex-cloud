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
using Vortex.Database.Entities.Achievements;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Snapshots;

namespace Vortex.Players.Grains;

/// <summary>
/// Loads every achievement definition once and caches it for the lifetime of the (kept-alive)
/// singleton, so per-player grains resolve their progress against in-memory definitions instead of
/// re-querying the database on each activation.
/// </summary>
[KeepAlive]
internal sealed class AchievementManagerGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<AchievementManagerGrain> logger
) : Grain, IAchievementManagerGrain
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<AchievementManagerGrain> _logger = logger;

    private ImmutableArray<AchievementDefinitionSnapshot> _definitions =
        ImmutableArray<AchievementDefinitionSnapshot>.Empty;
    private Dictionary<string, AchievementDefinitionSnapshot> _byName = new(
        StringComparer.OrdinalIgnoreCase
    );
    private string _defaultCategory = string.Empty;
    private bool _loaded;

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await LoadAsync(ct).ConfigureAwait(true);
        await base.OnActivateAsync(ct).ConfigureAwait(true);
    }

    public async Task<ImmutableArray<AchievementDefinitionSnapshot>> GetDefinitionsAsync(
        CancellationToken ct
    )
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(true);
        return _definitions;
    }

    public async Task<AchievementDefinitionSnapshot?> GetByNameAsync(
        string name,
        CancellationToken ct
    )
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(true);
        return _byName.TryGetValue(name, out AchievementDefinitionSnapshot? def) ? def : null;
    }

    public async Task<string> GetDefaultCategoryAsync(CancellationToken ct)
    {
        await EnsureLoadedAsync(ct).ConfigureAwait(true);
        return _defaultCategory;
    }

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

            List<AchievementEntity> achievements = await dbCtx
                .Achievements.AsNoTracking()
                .OrderBy(a => a.Id)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            List<AchievementLevelEntity> levels = await dbCtx
                .AchievementLevels.AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(true);

            Dictionary<int, ImmutableArray<AchievementLevelSnapshot>> levelsByAchievement = levels
                .GroupBy(l => l.AchievementEntityId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                        g.OrderBy(l => l.Level)
                            .Select(l => new AchievementLevelSnapshot
                            {
                                Level = l.Level,
                                BadgeCode = l.BadgeCode,
                                ProgressRequirement = l.ProgressRequirement,
                                RewardAmount = l.RewardAmount,
                                RewardType = l.RewardType,
                                ScorePoints = l.ScorePoints,
                            })
                            .ToImmutableArray()
                );

            ImmutableArray<AchievementDefinitionSnapshot>.Builder builder =
                ImmutableArray.CreateBuilder<AchievementDefinitionSnapshot>();

            foreach (AchievementEntity achievement in achievements)
            {
                if (
                    !levelsByAchievement.TryGetValue(
                        achievement.Id,
                        out ImmutableArray<AchievementLevelSnapshot> achievementLevels
                    )
                    || achievementLevels.Length == 0
                )
                {
                    // A header with no levels can never be progressed or displayed; skip it.
                    continue;
                }

                builder.Add(
                    new AchievementDefinitionSnapshot
                    {
                        Id = achievement.Id,
                        Name = achievement.Name,
                        Category = achievement.Category,
                        DisplayMethod = achievement.DisplayMethod,
                        Levels = achievementLevels,
                    }
                );
            }

            _definitions = builder.ToImmutable();
            _byName = _definitions.ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);
            _defaultCategory = _definitions.IsEmpty ? string.Empty : _definitions[0].Category;
            _loaded = true;

            _logger.LogInformation(
                "Loaded {Count} achievement definitions into cache.",
                _definitions.Length
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load achievement definitions.");
        }
    }
}
