using Orleans;

namespace Vortex.Primitives.Pets.Snapshots;

/// <summary>A parent as the breeding-confirmation dialog lists it, in the order the client reads.</summary>
[GenerateSerializer, Immutable]
public sealed record PetBreedingParentSnapshot
{
    [Id(0)]
    public required int WebId { get; init; }

    [Id(1)]
    public required string Name { get; init; }

    [Id(2)]
    public required int Level { get; init; }

    /// <summary>Pet figure as a string here, unlike the block form used elsewhere: type, breed and
    /// colour separated by spaces.</summary>
    [Id(3)]
    public required string Figure { get; init; }

    [Id(4)]
    public required string Owner { get; init; }
}
