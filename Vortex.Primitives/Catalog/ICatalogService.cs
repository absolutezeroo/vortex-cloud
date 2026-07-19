using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Catalog.Snapshots;

namespace Vortex.Primitives.Catalog;

public interface ICatalogService
{
    public CatalogSnapshot GetCatalogSnapshot(CatalogType catalogType);
}
