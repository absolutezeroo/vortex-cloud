using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Badges;

[GenerateSerializer, Immutable]
public sealed record IsBadgeRequestFulfilledEventMessageComposer : IComposer
{
    [Id(0)]
    public required string RequestCode { get; init; }

    [Id(1)]
    public required bool Fulfilled { get; init; }
}
