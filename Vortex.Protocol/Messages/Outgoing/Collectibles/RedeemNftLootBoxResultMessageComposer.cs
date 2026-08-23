using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Collectibles;

/// <summary>
/// How opening a loot box ended. One short, and the client names all three codes: 0 opened, 1
/// failed, 2 the box is not in the stardust wallet.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RedeemNftLootBoxResultMessageComposer : IComposer
{
    [Id(0)]
    public required short Status { get; init; }

    public const short Success = 0;
    public const short Fail = 1;
    public const short NotInStarDustWallet = 2;
}
