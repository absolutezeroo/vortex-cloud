using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Bots;

[GenerateSerializer, Immutable]
public sealed record BotRemovedFromInventoryEventMessageComposer : IComposer
{
    [Id(0)]
    public required int BotId { get; init; }
}
