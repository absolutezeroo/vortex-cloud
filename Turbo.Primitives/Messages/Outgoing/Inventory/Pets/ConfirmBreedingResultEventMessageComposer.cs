using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Inventory.Pets;

[GenerateSerializer, Immutable]
public sealed record ConfirmBreedingResultEventMessageComposer : IComposer
{
    [Id(0)]
    public required bool Success { get; init; }

    [Id(1)]
    public required int NewPetId { get; init; }
}
