using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Inventory.Pets;

[GenerateSerializer, Immutable]
public sealed record ConfirmBreedingRequestEventMessageComposer : IComposer
{
    [Id(0)]
    public required int PetOneId { get; init; }

    [Id(1)]
    public required int PetTwoId { get; init; }

    [Id(2)]
    public required int OwnerOneId { get; init; }

    [Id(3)]
    public required int OwnerTwoId { get; init; }

    [Id(4)]
    public required int ProposedRace { get; init; }

    [Id(5)]
    public required string ProposedColor { get; init; }

    [Id(6)]
    public required int ProposedGender { get; init; }
}
