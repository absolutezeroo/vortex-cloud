using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Pets;

[GenerateSerializer, Immutable]
public sealed record NestBreedingSuccessEventMessageComposer : IComposer
{
    [Id(0)]
    public required int NewPetId { get; init; }
}
