using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Collectibles;

/// <summary>
/// How many stamps the player holds. Asked per wallet, and re-asked every time the minting tab's
/// active wallet changes.
/// </summary>
public record GetCollectibleMintTokensMessage : IMessageEvent
{
    public required string Wallet { get; init; }
}
