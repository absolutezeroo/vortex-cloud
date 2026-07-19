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
}
