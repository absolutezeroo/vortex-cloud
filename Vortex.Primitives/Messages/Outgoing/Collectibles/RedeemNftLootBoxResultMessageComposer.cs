using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Collectibles;

/// <summary>
/// How opening a loot box ended. One short, same as the claim result and with the same caveat: the
/// client stores the code without interpreting it, so the meanings are not recoverable from it.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RedeemNftLootBoxResultMessageComposer : IComposer
{
    [Id(0)]
    public required short Status { get; init; }
}
