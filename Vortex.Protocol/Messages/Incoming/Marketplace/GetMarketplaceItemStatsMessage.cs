using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Marketplace;

public record GetMarketplaceItemStatsMessage : IMessageEvent
{
    public int CategoryId { get; init; }
    public int TypeId { get; init; }
}
