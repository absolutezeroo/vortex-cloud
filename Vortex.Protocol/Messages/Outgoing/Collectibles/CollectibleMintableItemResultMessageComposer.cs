using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Collectibles;

/// <summary>
/// How a mint attempt ended. One short.
/// </summary>
/// <remarks>
/// <para>
/// The codes are the client's own, declared as three constants on its parser, and success is
/// <b>1</b> — not 0. That is the reverse of every other reply in this domain (a store purchase, a
/// claim and a transfer all succeed on 0), and the same obfuscated constant name means 0 in one
/// parser and 1 in this one, so nothing here can be carried across from a sibling message.
/// </para>
/// <para>
/// Only <see cref="Success"/> is treated as success: the tab compares the code to it and shows the
/// failure notification for anything else. Sending the wrong constant is silent — the player is told
/// a Relic was minted, and no Relic exists.
/// </para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record CollectibleMintableItemResultMessageComposer : IComposer
{
    [Id(0)]
    public required short Status { get; init; }

    /// <summary>The attempt was refused. The client shows its mint-failed notification.</summary>
    public const short Failed = 0;

    /// <summary>The furniture became a Relic.</summary>
    public const short Success = 1;

    /// <summary>The third code the client declares. It is indistinguishable from
    /// <see cref="Failed"/> to the player, so it is used only where the wallet is the reason.</summary>
    public const short NotInStardustWallet = 2;
}
