using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.MysteryBox.Snapshots;

namespace Vortex.Primitives.MysteryBox.Grains;

/// <summary>
/// Singleton grain caching the mystery box reference data: which furniture definitions run the
/// mystery box logic, and the weighted prize pools. Both are read on every open and every tracker
/// push, so re-querying them per event would put table scans on the hot path for data that changes
/// only when an admin edits it. Mirrors <c>IQuestManagerGrain</c>.
///
/// A box's <em>colour</em> is deliberately not here: it lives in the furniture instance's state (see
/// <see cref="MysteryBoxSprite"/>), so one definition covers all eight colours.
/// </summary>
public interface IMysteryBoxManagerGrain : IGrainWithStringKey
{
    /// <summary>Every furniture definition whose logic is <c>furniture_mysterybox</c>.</summary>
    public Task<ImmutableArray<int>> GetBoxDefinitionIdsAsync(CancellationToken ct);

    /// <summary>True when <paramref name="definitionId"/> is a mystery box.</summary>
    public Task<bool> IsBoxDefinitionAsync(int definitionId, CancellationToken ct);

    /// <summary>Draws a prize from <paramref name="pool"/>, restricted to <paramref name="color"/>
    /// (empty for the trophy pool). Null when the pool holds nothing eligible.</summary>
    public Task<MysteryBoxPrizeSnapshot?> PickPrizeAsync(
        MysteryBoxPrizePool pool,
        string color,
        CancellationToken ct
    );

    /// <summary>Re-reads the tables into the cache, so admin edits go live without a restart.</summary>
    public Task ReloadAsync(CancellationToken ct);
}
