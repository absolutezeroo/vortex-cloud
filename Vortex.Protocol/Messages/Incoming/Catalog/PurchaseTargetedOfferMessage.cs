using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Catalog;

public record PurchaseTargetedOfferMessage : IMessageEvent
{
    public int OfferId { get; init; }
    public int Quantity { get; init; }
}
