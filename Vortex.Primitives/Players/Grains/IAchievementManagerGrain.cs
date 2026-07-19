using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Players.Snapshots;

namespace Vortex.Primitives.Players.Grains;

/// <summary>
/// Singleton grain that loads and caches every achievement definition (headers + levels) from the
/// database, so per-player grains never re-query definitions on activation.
/// </summary>
public interface IAchievementManagerGrain : IGrainWithStringKey
{
    public Task<ImmutableArray<AchievementDefinitionSnapshot>> GetDefinitionsAsync(
        CancellationToken ct
    );

    /// <summary>Looks up a definition by its <c>Name</c> (case-insensitive), used by progression.</summary>
    public Task<AchievementDefinitionSnapshot?> GetByNameAsync(string name, CancellationToken ct);

    /// <summary>The category tab the client should open by default in the achievements window.</summary>
    public Task<string> GetDefaultCategoryAsync(CancellationToken ct);
}
