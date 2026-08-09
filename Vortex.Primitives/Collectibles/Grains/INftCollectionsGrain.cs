using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Players;

namespace Vortex.Primitives.Collectibles.Grains;

/// <summary>
/// The hotel's collections, and where a given player stands in them. Singleton, and caching the
/// collection definitions the way <c>IMysteryBoxManagerGrain</c> caches its own: they change when an
/// admin edits them, not while anybody is playing.
/// <para>
/// What a player owns is <em>not</em> cached — it changes with every purchase, trade and sale — so
/// it is counted per request against their furniture.
/// </para>
/// </summary>
public interface INftCollectionsGrain : IGrainWithStringKey
{
    /// <summary>
    /// Every collection, filled in for one player: how many of each item they hold, what that
    /// scores, and whether the reward at the end is theirs to take.
    /// </summary>
    public Task<ImmutableArray<NftCollectionSnapshot>> GetCollectionsForPlayerAsync(
        PlayerId playerId,
        CancellationToken ct
    );

    /// <summary>
    /// A player's collector standing. The highest score is remembered, because the live one falls
    /// when furniture is sold and the client shows both side by side.
    /// </summary>
    public Task<CollectorScoreSnapshot> GetCollectorScoreAsync(
        PlayerId playerId,
        CancellationToken ct
    );

    /// <summary>Re-reads the collections, so an admin's edits go live without a restart.</summary>
    public Task ReloadAsync(CancellationToken ct);
}
