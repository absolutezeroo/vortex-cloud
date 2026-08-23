using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Collectibles;

/// <summary>
/// How a wallet transfer ended.
///
/// One short, and the sign of it matters more than usual: the client reads success as
/// <c>resultCode == 0</c>. This composer used to have no fields at all, so it went out as an empty
/// body — which reads as zero, which reads as "transferred". A refusal has to carry a non-zero
/// code, and the client prints that code into its error line.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record NftTransferAssetsResultMessageComposer : IComposer
{
    /// <summary>Zero means the transfer went through. Anything else is shown to the player as the
    /// id in "collectibles.transfer.error".</summary>
    [Id(0)]
    public required short ResultCode { get; init; }

    /// <summary>No chain, no wallet, no contract — nothing to transfer on. Any non-zero value would
    /// do; this one is simply not zero.</summary>
    public const short NotAvailable = 1;
}
