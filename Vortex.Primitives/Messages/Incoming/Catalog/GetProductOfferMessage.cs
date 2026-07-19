using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Catalog;

public record GetProductOfferMessage : IMessageEvent
{
    public int OfferId { get; init; }
}
