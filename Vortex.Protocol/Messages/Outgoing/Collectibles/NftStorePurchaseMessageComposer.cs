using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Collectibles;

/// <summary>
/// How a shop purchase ended. One short, and only two values are distinguishable to the client: it
/// alerts on <see cref="Error"/> and celebrates on anything else.
/// </summary>
/// <remarks>
/// Note which way round that is. Zero is success, as it is for the transfer and the claim, so an
/// unwritten body or a defaulted enum reads as "bought" — the failure code is the one that has to be
/// deliberate.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record NftStorePurchaseMessageComposer : IComposer
{
    [Id(0)]
    public required short Result { get; init; }

    public const short Success = 0;

    /// <summary>The only code the client tells apart: it raises the purchase-error alert.</summary>
    public const short Error = 1;
}
