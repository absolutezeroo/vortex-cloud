using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Collectibles;

public record NftCollectiblesClaimBonusItemMessage : IMessageEvent
{
    /// <summary>Which collection the bonus is being claimed from
    /// (CollectionView.as:186 sends <c>nftCollection.collectionId</c>).</summary>
    public string CollectionId { get; init; } = string.Empty;

    /// <summary>The wallet to credit — the view's <c>activeWallet</c>.</summary>
    public string WalletAddress { get; init; } = string.Empty;
}
