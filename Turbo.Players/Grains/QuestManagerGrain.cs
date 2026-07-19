using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Quests;
using Turbo.Primitives.Quests.Grains;
using Turbo.Primitives.Quests.Snapshots;

namespace Turbo.Players.Grains;

/// <summary>
/// Loads every quest definition once and caches it for the lifetime of the kept-alive singleton, so
/// per-player quest grains resolve against in-memory definitions instead of re-querying per read.
/// </summary>
[KeepAlive]
internal sealed class QuestManagerGrain(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    ILogger<QuestManagerGrain> logger
) : Grain, IQuestManagerGrain
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<QuestManagerGrain> _logger = logger;

    private ImmutableArray<QuestDefinitionSnapshot> _definitions =
        ImmutableArray<QuestDefinitionSnapshot>.Empty;
    private bool _loaded;

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await LoadAsync(ct).ConfigureAwait(true);
        await base.OnActivateAsync(ct).ConfigureAwait(true);
    }

    public async Task<ImmutableArray<QuestDefinitionSnapshot>> GetDefinitionsAsync(
        CancellationToken ct
    )
    {
        if (!_loaded)
        {
            await LoadAsync(ct).ConfigureAwait(true);
        }

        return _definitions;
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        try
        {
            await using TurboDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            _definitions =
            [
                .. (
                    await dbCtx
                        .Quests.AsNoTracking()
                        .OrderBy(q => q.SortOrder)
                        .ThenBy(q => q.Id)
                        .ToListAsync(ct)
                        .ConfigureAwait(true)
                ).Select(q => new QuestDefinitionSnapshot
                {
                    Id = q.Id,
                    CampaignCode = q.CampaignCode,
                    ChainCode = q.ChainCode,
                    LocalizationCode = q.LocalizationCode,
                    QuestType = q.QuestType,
                    TotalSteps = Math.Max(1, q.TotalSteps),
                    RewardType = q.RewardType,
                    RewardAmount = q.RewardAmount,
                    CatalogPageName = q.CatalogPageName,
                    ImageVersion = q.ImageVersion,
                    SortOrder = q.SortOrder,
                    Easy = q.Easy,
                    Seasonal = q.Seasonal,
                    SeasonalSeconds = q.SeasonalSeconds,
                    EndsAt = q.EndsAt,
                }),
            ];

            _loaded = true;
            _logger.LogInformation(
                "Loaded {Count} quest definitions into cache.",
                _definitions.Length
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load quest definitions.");
        }
    }
}
