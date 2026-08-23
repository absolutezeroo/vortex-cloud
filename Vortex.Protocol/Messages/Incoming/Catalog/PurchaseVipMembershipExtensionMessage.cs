using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Catalog;

public record PurchaseVipMembershipExtensionMessage : IMessageEvent
{
    public int OfferId { get; init; }
}
