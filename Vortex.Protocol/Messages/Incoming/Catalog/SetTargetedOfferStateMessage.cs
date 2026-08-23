using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Catalog;

public record SetTargetedOfferStateMessage : IMessageEvent
{
    public int TargetedOfferId { get; init; }
    public int TrackingState { get; init; }
}
