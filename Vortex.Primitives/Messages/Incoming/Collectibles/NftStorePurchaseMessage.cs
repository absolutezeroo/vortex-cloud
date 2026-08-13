using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Collectibles;

/// <summary>
/// Buying one thing from the Collectors Guild shop.
/// </summary>
/// <remarks>
/// The offer is named by its product code, not by a row id: the client's offer struct carries no id
/// at all, and its purchase dialog sends the code straight back. The wallet address rides along
/// because the real Habbo credits the item to a chain wallet; here it is only ever logged.
/// </remarks>
public record NftStorePurchaseMessage : IMessageEvent
{
    public required string ProductCode { get; init; }

    public required string Wallet { get; init; }
}
