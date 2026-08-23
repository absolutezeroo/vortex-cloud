using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.Protocol.Messages.Outgoing.Inventory.Pets;

[GenerateSerializer, Immutable]
public sealed record ConfirmBreedingResultEventMessageComposer : IComposer
{
    [Id(0)]
    public required int BreedingNestStuffId { get; init; }

    [Id(1)]
    public required int Result { get; init; }
}
