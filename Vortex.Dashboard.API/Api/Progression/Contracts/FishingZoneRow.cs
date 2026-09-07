namespace Vortex.Dashboard.API.Api.Progression.Contracts;

/// <summary>
/// One fishing spot.
/// </summary>
/// <param name="SpeciesCount">How many species this zone can yield. Zero is the one
/// misconfiguration here that looks exactly like a bug from the outside: the spot can be fished and
/// never gives anything.</param>
/// <param name="FurniIconUrl">The spot's artwork, so the operator recognises the furniture rather
/// than reading a classname. Null when the hotel has no furni renderer configured.</param>
public sealed record FishingZoneRow(
    int Id,
    string NameKey,
    string FurniClass,
    string? FurniIconUrl,
    int RequiredLevel,
    int MinCatches,
    int MaxCatches,
    int SpeciesCount
);
