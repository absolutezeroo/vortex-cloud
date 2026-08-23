using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record ClubGiftInfoEventMessageComposer : IComposer
{
    [Id(0)]
    public required int DaysUntilNextGift { get; init; }

    [Id(1)]
    public required int GiftsAvailable { get; init; }

    [Id(2)]
    public required IReadOnlyList<CatalogOfferSnapshot> Offers { get; init; }

    [Id(3)]
    public required IReadOnlyList<ClubGiftOfferData> GiftData { get; init; }
}
