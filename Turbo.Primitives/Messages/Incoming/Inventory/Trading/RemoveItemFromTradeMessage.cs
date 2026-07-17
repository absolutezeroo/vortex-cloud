using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Inventory.Trading;

public record RemoveItemFromTradeMessage : IMessageEvent
{
    /// <summary>Inventory furniture item id the requester wants to pull back from their offer.</summary>
    public required int ItemId { get; init; }
}
