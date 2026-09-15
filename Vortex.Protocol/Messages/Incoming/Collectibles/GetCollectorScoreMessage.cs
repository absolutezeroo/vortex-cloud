using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Collectibles;

public record GetCollectorScoreMessage : IMessageEvent
{
    /// <summary>
    /// The wallet the collectibles view currently has selected (CollectiblesView.as:310 sends
    /// <c>activeWallet</c>). The score answer carries a wallet address back, so this is the one it
    /// is meant to be scoped to.
    /// </summary>
    public string WalletAddress { get; init; } = string.Empty;
}
