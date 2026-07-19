using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Pets;

[GenerateSerializer, Immutable]
public sealed record ConfirmBreedingResultEventMessageComposer : IComposer
{
    [Id(0)]
    public required bool Success { get; init; }

    [Id(1)]
    public required int NewPetId { get; init; }
}
