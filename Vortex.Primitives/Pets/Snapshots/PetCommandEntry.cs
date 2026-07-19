namespace Vortex.Primitives.Pets.Snapshots;

public sealed record PetCommandEntry
{
    public required int PetType { get; init; }
    public required int CommandId { get; init; }
    public required int LevelRequired { get; init; }
    public required string Posture { get; init; }
    public required int EnergyCost { get; init; }
    public required int XpReward { get; init; }
}
