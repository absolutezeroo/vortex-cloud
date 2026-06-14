using System.Collections.Generic;
using Orleans;
using Turbo.Primitives.Marketplace.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Marketplace;

[GenerateSerializer, Immutable]
public sealed record MarketPlaceOwnOffersEventMessageComposer : IComposer
{
    [Id(0)] public required int CreditsWaiting { get; init; }
    [Id(1)] public required List<MarketplaceOfferSnapshot> Offers { get; init; }
}
