namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One furniture definition that behaves as a mystery box.
/// </summary>
/// <param name="TotalStates">How many colours this box has: a box's colour is its furniture state.</param>
public sealed record MysteryBoxDefinitionRow(
    int Id,
    string Name,
    int SpriteId,
    int TotalStates,
    string? FurnitureIconUrl
);
