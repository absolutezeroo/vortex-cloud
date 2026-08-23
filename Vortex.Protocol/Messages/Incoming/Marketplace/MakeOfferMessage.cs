using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Marketplace;

public record MakeOfferMessage : IMessageEvent
{
    public int Price { get; init; }
    public int FurniType { get; init; }
    public int FurnitureItemId { get; init; }
}
