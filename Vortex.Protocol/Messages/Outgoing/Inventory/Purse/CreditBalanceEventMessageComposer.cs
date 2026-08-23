using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Inventory.Purse;

[GenerateSerializer, Immutable]
public sealed record CreditBalanceEventMessageComposer : IComposer
{
    [Id(0)]
    public required string Balance { get; init; }
}
