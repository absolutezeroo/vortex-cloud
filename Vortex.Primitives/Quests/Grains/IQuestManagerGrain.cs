using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Quests.Snapshots;

namespace Vortex.Primitives.Quests.Grains;

/// <summary>
/// Singleton grain that loads and caches every quest definition from the database, so per-player
/// grains resolve their progress against in-memory definitions.
/// </summary>
public interface IQuestManagerGrain : IGrainWithStringKey
{
    public Task<ImmutableArray<QuestDefinitionSnapshot>> GetDefinitionsAsync(CancellationToken ct);

    /// <summary>
    /// Re-reads every quest definition from the database into the cache. Called by the dashboard admin
    /// service after a write so the live quests never drift from the database, without an emulator
    /// restart (mirrors the targeted-offer manager's reload).
    /// </summary>
    public Task ReloadAsync(CancellationToken ct);
}
