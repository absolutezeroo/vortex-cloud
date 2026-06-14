using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Marketplace;

public record CancelMarketplaceOfferMessage : IMessageEvent
{
    public int OfferId { get; init; }
}
