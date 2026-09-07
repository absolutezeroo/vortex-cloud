namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// One furniture definition in the picker.
/// </summary>
/// <param name="Logic">What the thing does, which is the one filter that answers "I forgot the name
/// but I know what it does".</param>
/// <param name="IconUrl">Built from the name; null when the hotel has no furni renderer configured.</param>
public sealed record FurnitureDirectoryRow(
    int Id,
    int SpriteId,
    string Name,
    string Logic,
    string Type,
    string Category,
    int Width,
    int Length,
    bool CanTrade,
    bool CanSell,
    string? IconUrl
);
