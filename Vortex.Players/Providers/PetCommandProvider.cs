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

public sealed class PetCommandProvider(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<IPetCommandProvider> logger
) : IPetCommandProvider
{
    private ImmutableDictionary<int, ImmutableArray<PetCommandEntry>> _byType = ImmutableDictionary<
        int,
        ImmutableArray<PetCommandEntry>
    >.Empty;

    /// <summary>Command display name (lower-cased) to command id, for resolving a chat order.</summary>
    private ImmutableDictionary<string, int> _idsByName = ImmutableDictionary<string, int>.Empty;
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
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

    /// <summary>
    /// Resolves the command a player typed. AS3 has no "issue pet command" packet: PetCommandTool
    /// posts the order as ordinary chat, "&lt;pet name&gt; &lt;localised command&gt;", so the words
    /// here are the exact strings the client prints on its training buttons.
    /// </summary>
    public int? ResolveCommandIdByName(int petType, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (!_idsByName.TryGetValue(name.Trim().ToLowerInvariant(), out int commandId))
        {
            return null;
        }

        // A name is global, but a pet only knows the commands configured for its own type.
        return GetCommandConfig(petType, commandId) is null ? null : commandId;
    }

    public async Task ReloadAsync(CancellationToken ct)
    {
        VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

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

            List<PetCommandNameEntity> nameEntities = await dbCtx
                .PetCommandNames.AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            _idsByName = nameEntities
                .GroupBy(e => e.Name.Trim().ToLowerInvariant())
                .ToImmutableDictionary(g => g.Key, g => g.First().CommandId);

            _logger.LogInformation(
                "Loaded pet commands: {Count} entries across {Types} types, {Names} command names",
                entities.Count,
                byType.Count,
                _idsByName.Count
            );
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(false);
        }
    }
}
