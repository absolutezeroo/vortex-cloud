using Orleans;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Bots;

[GenerateSerializer, Immutable]
public sealed record BotAddedToInventoryEventMessageComposer : IComposer
{
    [Id(0)]
    public required BotSnapshot Bot { get; init; }

    /// <summary>Whether the client should pop the inventory open on top of adding the row — true
    /// when the player did something to cause it, false when a bot simply came back from a room.</summary>
    [Id(1)]
    public bool OpenInventory { get; init; }
}
