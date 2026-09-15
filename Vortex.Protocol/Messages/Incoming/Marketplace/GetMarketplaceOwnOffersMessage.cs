using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Marketplace;

public record GetMarketplaceOwnOffersMessage : IMessageEvent
{
    /// <summary>
    /// One int the client always defaults to 1 (<c>requestOwnItems(param1:int = 1)</c>,
    /// MarketPlaceLogic.as:180). It stores the value but never branches on it in any reachable
    /// path, so what it selects is not recoverable from the client — read so the wire stays in
    /// sync, and left explicitly unnamed rather than guessed at.
    /// </summary>
    public int Unknown1 { get; init; } = 1;
}
