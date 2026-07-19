using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Marketplace.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Marketplace;

[GenerateSerializer, Immutable]
public sealed record MarketPlaceOffersEventMessageComposer : IComposer
{
    [Id(0)]
    public required List<MarketplaceOfferSnapshot> Offers { get; init; }

    [Id(1)]
    public required int TotalFound { get; init; }
}
