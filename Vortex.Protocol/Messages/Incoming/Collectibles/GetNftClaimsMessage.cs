using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Collectibles;

/// <summary>
/// "What is waiting on this wallet?" — the claims tab. Carries the wallet address; the parser used
/// to not exist at all, so the request was dropped before anything could read it.
/// </summary>
public record GetNftClaimsMessage : IMessageEvent
{
    public required string Wallet { get; init; }
}
