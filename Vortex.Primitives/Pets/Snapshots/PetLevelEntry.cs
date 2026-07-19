namespace Vortex.Primitives.Pets.Snapshots;

public sealed record PetLevelEntry
{
    public required int PetType { get; init; }
    public required int Level { get; init; }
    public required int ExperienceRequired { get; init; }
    public required int EnergyCap { get; init; }
    public required int NutritionCap { get; init; }
}
