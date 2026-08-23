using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Collectibles;

/// <summary>
/// What the collectibles shop tab has for sale.
///
/// An empty list is a real answer and the one this hotel gives: the tab renders "nothing here" and
/// marks itself ready. Not answering at all is the failure mode — the tab sets a waiting flag when
/// it asks and only clears it here, so a dropped request leaves it loading for as long as it is
/// open.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record NftStoreOffersMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<NftStoreOfferSnapshot> Offers { get; init; }
}
