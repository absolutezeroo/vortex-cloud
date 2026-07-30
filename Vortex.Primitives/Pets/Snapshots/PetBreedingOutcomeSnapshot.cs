using Orleans;

namespace Vortex.Primitives.Pets.Snapshots;

/// <summary>One side of the breeding outcome dialog, in the order the client reads it.</summary>
[GenerateSerializer, Immutable]
public sealed record PetBreedingOutcomeSnapshot
{
    [Id(0)]
    public required int StuffId { get; init; }

    [Id(1)]
    public required int ClassId { get; init; }

    [Id(2)]
    public string ProductCode { get; init; } = string.Empty;

    [Id(3)]
    public required int UserId { get; init; }

    [Id(4)]
    public string UserName { get; init; } = string.Empty;

    [Id(5)]
    public int RarityLevel { get; init; }

    [Id(6)]
    public bool HasMutation { get; init; }
}
