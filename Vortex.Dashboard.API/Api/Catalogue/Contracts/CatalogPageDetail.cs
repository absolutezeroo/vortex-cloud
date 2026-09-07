using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One catalogue page with what it sells.
/// </summary>
/// <param name="ImageData">The layout's own strings, as the page stores them. Null for a layout
/// that takes none.</param>
public sealed record CatalogPageDetail(
    int Id,
    int CatalogType,
    int? ParentEntityId,
    string? ParentLocalization,
    string Localization,
    string? Name,
    int Icon,
    string? IconUrl,
    string Layout,
    IReadOnlyList<string>? ImageData,
    IReadOnlyList<string>? TextData,
    int SortOrder,
    bool Visible,
    IReadOnlyList<CatalogOfferRow> Offers
);
