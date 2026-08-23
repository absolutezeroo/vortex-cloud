using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Inventory.Pets;

[GenerateSerializer, Immutable]
public sealed record GoToBreedingNestFailureEventMessageComposer : IComposer
{
    [Id(0)]
    public required int Reason { get; init; }
}
