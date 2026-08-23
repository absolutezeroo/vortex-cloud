using Orleans;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Catalog;

[GenerateSerializer, Immutable]
public sealed record PurchaseNotAllowedMessageComposer : IComposer
{
    [Id(0)]
    public required CatalogPurchaseErrorType ErrorType { get; init; }
}
