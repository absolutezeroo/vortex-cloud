using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Catalog;

public record GetIsOfferGiftableMessage : IMessageEvent
{
    public int OfferId { get; init; }
}
