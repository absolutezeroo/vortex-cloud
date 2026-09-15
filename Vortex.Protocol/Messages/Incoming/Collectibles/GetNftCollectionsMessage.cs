using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Collectibles;

public record GetNftCollectionsMessage : IMessageEvent
{
    /// <summary>The wallet whose collections the tab is listing; the client sends "" when no wallet
    /// is selected (CollectionsTab.as:425-430).</summary>
    public string WalletAddress { get; init; } = string.Empty;
}
