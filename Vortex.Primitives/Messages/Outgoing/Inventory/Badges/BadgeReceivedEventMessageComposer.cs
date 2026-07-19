using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Badges;

[GenerateSerializer, Immutable]
public sealed record BadgeReceivedEventMessageComposer : IComposer
{
    [Id(0)]
    public required int SlotId { get; init; }

    [Id(1)]
    public required string BadgeCode { get; init; }
}
