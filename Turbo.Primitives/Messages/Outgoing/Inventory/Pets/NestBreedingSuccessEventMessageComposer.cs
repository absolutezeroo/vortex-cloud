using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Inventory.Pets;

[GenerateSerializer, Immutable]
public sealed record NestBreedingSuccessEventMessageComposer : IComposer
{
    [Id(0)]
    public required int NewPetId { get; init; }
}
