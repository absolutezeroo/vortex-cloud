using System.Collections.Generic;
using System.Collections.Immutable;
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

public sealed class PetCommandProvider(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    ILogger<IPetCommandProvider> logger
) : IPetCommandProvider
{
    private ImmutableDictionary<int, ImmutableArray<PetCommandEntry>> _byType = ImmutableDictionary<
        int,
        ImmutableArray<PetCommandEntry>
    >.Empty;
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<IPetCommandProvider> _logger = logger;

    public IReadOnlyList<PetCommandEntry> GetCommandsForType(int petType)
    {
        if (_byType.TryGetValue(petType, out ImmutableArray<PetCommandEntry> entries))
        {
            return entries;
        }

        return [];
    }

    public ImmutableArray<int> GetAllCommandIds(int petType)
    {
        IReadOnlyList<PetCommandEntry> entries = GetCommandsForType(petType);

        return entries.Select(e => e.CommandId).ToImmutableArray();
    }

    public PetCommandEntry? GetCommandConfig(int petType, int commandId)
    {
        IReadOnlyList<PetCommandEntry> entries = GetCommandsForType(petType);

        foreach (PetCommandEntry entry in entries)
        {
            if (entry.CommandId == commandId)
            {
                return entry;
            }
        }

        return null;
    }

    public ImmutableArray<int> GetEnabledCommandIds(int petType, int petLevel)
    {
        IReadOnlyList<PetCommandEntry> entries = GetCommandsForType(petType);

        return entries
            .Where(e => e.LevelRequired <= petLevel)
            .Select(e => e.CommandId)
            .ToImmutableArray();
    }

    public async Task ReloadAsync(CancellationToken ct)
    {
        TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            List<PetCommandEntity> entities = await dbCtx
                .PetCommands.AsNoTracking()
                .OrderBy(c => c.LevelRequired)
                .ThenBy(c => c.CommandId)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            ImmutableDictionary<int, ImmutableArray<PetCommandEntry>> byType = entities
                .GroupBy(e => e.PetType)
                .ToImmutableDictionary(
                    g => g.Key,
                    g =>
                        g.Select(entity => new PetCommandEntry
                            {
                                PetType = entity.PetType,
                                CommandId = entity.CommandId,
                                LevelRequired = entity.LevelRequired,
                                Posture = entity.Posture,
                                EnergyCost = entity.EnergyCost,
                                XpReward = entity.XpReward,
                            })
                            .ToImmutableArray()
                );

            _byType = byType;

            _logger.LogInformation(
                "Loaded pet commands: {Count} entries across {Types} types",
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
