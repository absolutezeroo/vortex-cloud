using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Catalog;

public record PurchaseFromCatalogMessage : IMessageEvent
{
    public int PageId { get; init; }
    public int OfferId { get; init; }
    public string? ExtraParam { get; init; }
    public int Quantity { get; init; }
}
