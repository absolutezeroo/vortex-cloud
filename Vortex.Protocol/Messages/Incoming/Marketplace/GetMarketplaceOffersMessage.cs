using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Marketplace;

public record GetMarketplaceOffersMessage : IMessageEvent
{
    public int MinPrice { get; init; }
    public int MaxPrice { get; init; }
    public string SearchQuery { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}
