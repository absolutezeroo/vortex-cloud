using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Polls.Snapshots;

namespace Vortex.Primitives.Polls.Grains;

/// <summary>
/// Singleton grain that loads every enabled survey — poll, questions and choices in one pass — and
/// caches the assembled tree, so per-player grains never rebuild it per request.
/// </summary>
public interface IPollManagerGrain : IGrainWithStringKey
{
    public Task<ImmutableArray<PollDefinitionSnapshot>> GetDefinitionsAsync(CancellationToken ct);

    /// <summary>The cached survey with this id, or null when it is unknown or disabled.</summary>
    public Task<PollDefinitionSnapshot?> GetDefinitionAsync(int pollId, CancellationToken ct);

    /// <summary>
    /// Re-reads every survey from the database. Called by the dashboard after an edit so live polls
    /// never drift from the database without an emulator restart.
    /// </summary>
    public Task ReloadAsync(CancellationToken ct);
}
