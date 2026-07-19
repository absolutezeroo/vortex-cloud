using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Catalog;

public record GetNextTargetedOfferMessage : IMessageEvent
{
    public int OfferId { get; init; }
}
