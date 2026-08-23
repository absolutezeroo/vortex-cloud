using Orleans;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record PurchaseOKMessageComposer : IComposer
{
    [Id(0)]
    public required CatalogOfferSnapshot Offer { get; init; }
}
