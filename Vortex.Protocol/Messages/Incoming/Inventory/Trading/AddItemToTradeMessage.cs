using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Inventory.Trading;

public record AddItemToTradeMessage : IMessageEvent
{
    /// <summary>Inventory furniture item id the requester wants to add to their trade offer.</summary>
    public required int ItemId { get; init; }
}
