using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Marketplace;

[GenerateSerializer, Immutable]
public sealed record MarketplaceCanMakeOfferResultMessageComposer : IComposer
{
    /// <summary>
    /// What the client does next, read as <c>resultCode - 1</c>: 1 opens the make-offer dialog,
    /// 2 "no trading privilege", 3 "no trading pass", 4 opens the buy-tokens dialog (which is what
    /// <see cref="TokenCount"/> feeds), 5 releases the held items. A bool could not express any of
    /// this, and the client read two integers off a one-byte packet.
    /// </summary>
    [Id(0)]
    public required int ResultCode { get; init; }

    [Id(1)]
    public int TokenCount { get; init; }
}
