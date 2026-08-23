using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Catalog;

public record GetProductOfferMessage : IMessageEvent
{
    public int OfferId { get; init; }
}
