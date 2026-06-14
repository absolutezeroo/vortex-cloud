using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Catalog.Snapshots;

namespace Turbo.Primitives.Catalog.Providers;

public interface ICatalogClubGiftProvider
{
    IReadOnlyList<CatalogOfferSnapshot> GetAll();
    CatalogOfferSnapshot? FindByProductCode(string productCode);
    Task ReloadAsync(CancellationToken ct);
}
