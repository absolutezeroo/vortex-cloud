namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One prize a box can give.
/// </summary>
/// <param name="Color">Which box colour it belongs to; null means every colour of its pool.</param>
public sealed record MysteryBoxPrizeRow(
    int Id,
    string Pool,
    string? Color,
    string ProductType,
    int? FurnitureDefinitionId,
    string? ExtraParam,
    int Weight,
    bool Enabled,
    string? FurnitureName,
    string? FurnitureIconUrl
);
