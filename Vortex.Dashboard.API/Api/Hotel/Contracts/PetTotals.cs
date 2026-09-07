namespace Vortex.Dashboard.API.Api.Hotel.Contracts;

/// <summary>
/// The whole population, not the window: averages an operator reads to see whether pets are being
/// looked after, and how many were bred rather than bought.
/// </summary>
public sealed record PetTotals(
    int TotalPets,
    double AvgLevel,
    double AvgEnergy,
    double AvgNutrition,
    int BreedablePets,
    int BredPets
);
