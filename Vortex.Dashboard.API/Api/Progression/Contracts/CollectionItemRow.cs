namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One item of a collection.
/// </summary>
/// <param name="Resolved">Whether a furniture definition by this classname exists. False means the
/// item can be collected on paper and never actually held.</param>
public sealed record CollectionItemRow(
    int Id,
    string ProductCode,
    string ItemTypeId,
    int ProductTypeId,
    int Score,
    string Rarity,
    int SortOrder,
    bool Resolved,
    string? IconUrl
);
