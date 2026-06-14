using System.Collections.Generic;
using Orleans;
using Turbo.Primitives.Marketplace.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Marketplace;

[GenerateSerializer, Immutable]
public sealed record MarketPlaceOffersEventMessageComposer : IComposer
{
    [Id(0)]
    public required List<MarketplaceOfferSnapshot> Offers { get; init; }

    [Id(1)]
    public required int TotalFound { get; init; }
}
