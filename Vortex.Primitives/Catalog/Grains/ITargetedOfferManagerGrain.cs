using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Catalog.Snapshots;

namespace Vortex.Primitives.Catalog.Grains;

/// <summary>
/// Singleton grain caching every active targeted-offer definition (with its bundle products),
/// ordered for the client's "next offer" cycling.
/// </summary>
public interface ITargetedOfferManagerGrain : IGrainWithStringKey
{
    public Task<ImmutableArray<TargetedOfferDefinitionSnapshot>> GetDefinitionsAsync(
        CancellationToken ct
    );

    /// <summary>
    /// Re-reads every active offer + product from the database into the cache. Called by the dashboard
    /// admin service after a write so the live offers players see never drift from the database,
    /// without a full emulator restart (mirrors <c>ICatalogSnapshotProvider{TTag}.ReloadAsync</c>).
    /// </summary>
    public Task ReloadAsync(CancellationToken ct);
}
