using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Catalog;

public record ShopTargetedOfferViewedMessage : IMessageEvent
{
    public int TargetedOfferId { get; init; }
    public int TrackingState { get; init; }
}
