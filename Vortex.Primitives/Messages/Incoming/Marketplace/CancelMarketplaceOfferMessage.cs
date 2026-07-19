using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Marketplace;

public record CancelMarketplaceOfferMessage : IMessageEvent
{
    public int OfferId { get; init; }
}
