using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Bots;

[GenerateSerializer, Immutable]
public sealed record BotAddedToInventoryEventMessageComposer : IComposer
{
    // TODO: add properties if/when identified
}
