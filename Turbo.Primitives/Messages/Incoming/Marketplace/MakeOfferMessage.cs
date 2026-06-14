using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Marketplace;

public record MakeOfferMessage : IMessageEvent
{
    public int Price { get; init; }
    public int FurniType { get; init; }
    public int FurnitureItemId { get; init; }
}
