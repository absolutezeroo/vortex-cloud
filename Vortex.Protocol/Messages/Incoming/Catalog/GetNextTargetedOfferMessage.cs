using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Catalog;

public record GetNextTargetedOfferMessage : IMessageEvent
{
    public int OfferId { get; init; }
}
