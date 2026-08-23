using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Marketplace;

public record GetMarketplaceItemStatsMessage : IMessageEvent
{
    public int CategoryId { get; init; }
    public int TypeId { get; init; }
}
