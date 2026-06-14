using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Marketplace;

[GenerateSerializer, Immutable]
public sealed record MarketplaceItemStatsEventMessageComposer : IComposer
{
    [Id(0)] public required int AvgPrice { get; init; }
    [Id(1)] public required int OfferCount { get; init; }
    [Id(2)] public required int CategoryId { get; init; }
    [Id(3)] public required int TypeId { get; init; }
}
