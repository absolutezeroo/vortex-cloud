using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Inventory.Bots;

/// <summary>Every bot the player owns and has not placed in a room.</summary>
[GenerateSerializer, Immutable]
public sealed record BotInventoryEventMessageComposer : IComposer
{
    [Id(0)]
    public ImmutableArray<BotSnapshot> Bots { get; init; } = ImmutableArray<BotSnapshot>.Empty;
}
