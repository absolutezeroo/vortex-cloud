using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Collectibles;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Collectibles;

/// <summary>
/// The Relics on the table, as one side of a trade sees them.
/// </summary>
/// <remarks>
/// <para>
/// <b>Written from the receiver's point of view</b>, unlike the ordinary trade item list, which
/// names both sides by room-object id and can be sent to both players unchanged. Here the client
/// reads "mine" then "theirs" and believes it, so the two participants must be sent two different
/// packets with the lists swapped. Sending one of them the other's view silently hands each player
/// the other's Relics to look at — and the trade would still complete correctly, which is what makes
/// it hard to notice.
/// </para>
/// <para>
/// It also drives the inventory's lock state: the Collectibles tab greys out whatever appears here,
/// which is why the whole list is re-sent on every change rather than a delta.
/// </para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record TradeNftAssetsMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<CollectibleAssetSnapshot> MyAssets { get; init; }

    [Id(1)]
    public required ImmutableArray<CollectibleAssetSnapshot> TheirAssets { get; init; }
}
