using System.Collections.Generic;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Inventory.Trading;

public record AddItemsToTradeMessage : IMessageEvent
{
    /// <summary>Inventory furniture item ids to add to the trade offer in one batch (e.g. a whole
    /// groupable stack).</summary>
    public required IReadOnlyList<int> ItemIds { get; init; }
}
