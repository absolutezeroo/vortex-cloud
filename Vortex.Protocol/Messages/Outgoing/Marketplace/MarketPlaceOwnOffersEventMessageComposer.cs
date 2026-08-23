using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Marketplace.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Marketplace;

[GenerateSerializer, Immutable]
public sealed record MarketPlaceOwnOffersEventMessageComposer : IComposer
{
    [Id(0)]
    public required int CreditsWaiting { get; init; }

    [Id(1)]
    public required List<MarketplaceOfferSnapshot> Offers { get; init; }
}
