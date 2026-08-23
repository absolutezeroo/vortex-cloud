using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Catalog;

public record GetIsOfferGiftableMessage : IMessageEvent
{
    public int OfferId { get; init; }
}
