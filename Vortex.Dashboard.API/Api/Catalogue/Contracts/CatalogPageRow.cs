namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One catalogue page.
/// </summary>
/// <param name="ChildCount">Pages beneath it; <paramref name="OfferCount"/> is what it sells. A
/// page with neither is one a player can open onto nothing.</param>
public sealed record CatalogPageRow(
    int Id,
    int? ParentEntityId,
    string Localization,
    string? Name,
    int Icon,
    string? IconUrl,
    string Layout,
    int SortOrder,
    bool Visible,
    int ChildCount,
    int OfferCount
);
