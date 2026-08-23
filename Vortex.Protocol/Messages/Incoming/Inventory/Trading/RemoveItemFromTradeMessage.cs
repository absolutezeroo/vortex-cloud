using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Inventory.Trading;

public record RemoveItemFromTradeMessage : IMessageEvent
{
    /// <summary>Inventory furniture item id the requester wants to pull back from their offer.</summary>
    public required int ItemId { get; init; }
}
