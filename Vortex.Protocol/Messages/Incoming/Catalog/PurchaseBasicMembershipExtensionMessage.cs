using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Catalog;

public record PurchaseBasicMembershipExtensionMessage : IMessageEvent
{
    public int OfferId { get; init; }
}
