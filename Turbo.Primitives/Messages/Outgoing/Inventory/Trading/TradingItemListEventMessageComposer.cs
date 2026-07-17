using System.Collections.Immutable;
using Orleans;
using Turbo.Primitives.Inventory.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Inventory.Trading;

[GenerateSerializer, Immutable]
public sealed record TradingItemListEventMessageComposer : IComposer
{
    [Id(0)]
    public required int FirstUserId { get; init; }

    [Id(1)]
    public required ImmutableArray<FurnitureItemSnapshot> FirstUserItems { get; init; }

    [Id(2)]
    public required int FirstUserCredits { get; init; }

    [Id(3)]
    public required int SecondUserId { get; init; }

    [Id(4)]
    public required ImmutableArray<FurnitureItemSnapshot> SecondUserItems { get; init; }

    [Id(5)]
    public required int SecondUserCredits { get; init; }
}
