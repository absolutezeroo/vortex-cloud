using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Marketplace;

public record CancelMarketplaceOfferMessage : IMessageEvent
{
    public int OfferId { get; init; }
}
