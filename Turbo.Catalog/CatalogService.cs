using System;
using Microsoft.Extensions.Logging;
using Turbo.Primitives.Catalog;
using Turbo.Primitives.Catalog.Enums;
using Turbo.Primitives.Catalog.Providers;
using Turbo.Primitives.Catalog.Snapshots;
using Turbo.Primitives.Catalog.Tags;

namespace Turbo.Catalog;

public sealed class CatalogService(
    ILogger<ICatalogService> logger,
    ICatalogSnapshotProvider<NormalCatalog> normalCatalogProvider,
    ICatalogSnapshotProvider<BuildersClubCatalog> buildersClubCatalogProvider
) : ICatalogService
{
    private readonly ILogger<ICatalogService> _logger = logger;
    private readonly ICatalogSnapshotProvider<NormalCatalog> _normalCatalogProvider =
        normalCatalogProvider;
    private readonly ICatalogSnapshotProvider<BuildersClubCatalog> _buildersClubCatalogProvider =
        buildersClubCatalogProvider;

    public CatalogSnapshot GetCatalogSnapshot(CatalogType catalogType)
    {
        return catalogType switch
        {
            CatalogType.Normal => _normalCatalogProvider.Current,
            CatalogType.BuildersClub => _buildersClubCatalogProvider.Current,
            _ => throw new NotSupportedException($"Catalog type {catalogType} is not supported."),
        };
    }
}
