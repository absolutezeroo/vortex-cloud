using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Marketplace;

[GenerateSerializer, Immutable]
public sealed record MarketplaceConfigurationEventMessageComposer : IComposer
{
    [Id(0)]
    public required bool Enabled { get; init; }

    [Id(1)]
    public required int Commission { get; init; }

    [Id(2)]
    public required int Credits { get; init; }

    [Id(3)]
    public required int Advertisements { get; init; }

    [Id(4)]
    public required int MinimumPrice { get; init; }

    [Id(5)]
    public required int MaximumPrice { get; init; }

    [Id(6)]
    public required int OfferTime { get; init; }

    [Id(7)]
    public required int DisplayTime { get; init; }

    [Id(8)]
    public int SellingFeePercentage { get; init; }

    [Id(9)]
    public int RevenueLimit { get; init; }

    [Id(10)]
    public int HalfTaxLimit { get; init; }
}
