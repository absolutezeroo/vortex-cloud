using Orleans;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record ProductOfferEventMessageComposer : IComposer
{
    [Id(0)]
    public required CatalogOfferSnapshot Offer { get; init; }
}
