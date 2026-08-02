using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;

namespace Vortex.Primitives.MysteryBox.Grains;

/// <summary>
/// Singleton grain caching which furniture definitions run the mystery box logic. That set is read
/// on every open and every tracker push, so re-querying it per event would put a table scan on the
/// hot path for data that changes only when an admin edits it. Mirrors <c>IQuestManagerGrain</c>.
///
/// A box's <em>colour</em> is deliberately not here: it lives in the furniture instance's state (see
/// <see cref="MysteryBoxSprite"/>), so one definition covers all eight colours. The prizes are not
/// here either — they are a shared prize pool, drawn through
/// <see cref="Vortex.Primitives.Prizes.Grains.IPrizePoolManagerGrain"/>.
/// </summary>
public interface IMysteryBoxManagerGrain : IGrainWithStringKey
{
    /// <summary>Every furniture definition whose logic is <c>furniture_mysterybox</c>.</summary>
    public Task<ImmutableArray<int>> GetBoxDefinitionIdsAsync(CancellationToken ct);

    /// <summary>True when <paramref name="definitionId"/> is a mystery box.</summary>
    public Task<bool> IsBoxDefinitionAsync(int definitionId, CancellationToken ct);

    /// <summary>Re-reads the table into the cache, so admin edits go live without a restart.</summary>
    public Task ReloadAsync(CancellationToken ct);
}
