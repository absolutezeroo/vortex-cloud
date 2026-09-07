using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One level of the catalogue tree.
/// </summary>
/// <param name="ParentId">Which page this level sits under, null at the root.</param>
public sealed record CatalogPageList(
    int CatalogType,
    int? ParentId,
    int Count,
    IReadOnlyList<CatalogPageRow> Items
);
