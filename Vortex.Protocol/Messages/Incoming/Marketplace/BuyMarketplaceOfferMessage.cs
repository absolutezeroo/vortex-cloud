using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Marketplace;

public record BuyMarketplaceOfferMessage : IMessageEvent
{
    public int OfferId { get; init; }
}
