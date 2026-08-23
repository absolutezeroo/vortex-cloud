using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Pets;

/// <summary>
/// Asks the second owner to confirm a pairing: the nest, both parents, the rarity odds the dialog
/// charts, and what the offspring would be.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record ConfirmBreedingRequestEventMessageComposer : IComposer
{
    [Id(0)]
    public required int NestId { get; init; }

    [Id(1)]
    public required PetBreedingParentSnapshot PetOne { get; init; }

    [Id(2)]
    public required PetBreedingParentSnapshot PetTwo { get; init; }

    /// <summary>Odds per rarity tier. Breeding here has no rarity model, so the chart is empty
    /// rather than filled with invented percentages.</summary>
    [Id(3)]
    public required int ResultPetType { get; init; }
}
