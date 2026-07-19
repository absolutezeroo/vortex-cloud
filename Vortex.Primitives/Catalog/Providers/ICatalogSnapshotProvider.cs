using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Catalog.Snapshots;

namespace Vortex.Primitives.Catalog.Providers;

public interface ICatalogSnapshotProvider<TTag>
    where TTag : ICatalogTag
{
    public CatalogType CatalogType { get; }
    public CatalogSnapshot Current { get; }
    public Task<CatalogSnapshot> GetSnapshotAsync(CancellationToken ct);
    public Task ReloadAsync(CancellationToken ct);
}
