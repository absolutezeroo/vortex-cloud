using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Inventory.Pets;

[GenerateSerializer, Immutable]
public sealed record GoToBreedingNestFailureEventMessageComposer : IComposer
{
    [Id(0)]
    public required int Reason { get; init; }
}
