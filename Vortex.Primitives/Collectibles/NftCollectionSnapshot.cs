using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Collectibles;

/// <summary>
/// A collection as one viewer sees it: what is in it, how much of it they have, and whether the
/// bonus and reward at the end of it are still to be claimed.
/// <para>
/// The bonus and reward items are optional and each is announced by a boolean. Their claims are
/// written later in the packet, after the status, under those <em>same</em> two booleans — so a
/// collection with a reward but no bonus writes one item and one claim, in two separate places.
/// </para>
/// </summary>
[GenerateSerializer, Immutable]
public sealed record NftCollectionSnapshot
{
    [Id(0)]
    public required string CollectionId { get; init; }

    [Id(1)]
    public required string CollectionName { get; init; }

    [Id(2)]
    public ImmutableArray<CollectibleProductItemSnapshot> Items { get; init; } = [];

    /// <summary>What the viewer has scored in this collection.</summary>
    [Id(3)]
    public int CollectionScore { get; init; }

    /// <summary>What the whole collection is worth, which is what the client draws progress against.</summary>
    [Id(4)]
    public int CollectionTotalScore { get; init; }

    [Id(5)]
    public int CollectionBoostScore { get; init; }

    [Id(6)]
    public CollectibleProductItemSnapshot? BonusItem { get; init; }

    [Id(7)]
    public CollectibleProductItemSnapshot? RewardItem { get; init; }

    /// <summary>Unix milliseconds; a long on the wire.</summary>
    [Id(8)]
    public long ReleasedTimeMs { get; init; }

    [Id(9)]
    public long SnapshotTimeMs { get; init; }

    /// <summary>A short on the wire.</summary>
    [Id(10)]
    public int Status { get; init; }

    /// <summary>Written only when <see cref="BonusItem"/> is present.</summary>
    [Id(11)]
    public CollectibleItemClaimSnapshot? BonusItemClaim { get; init; }

    /// <summary>Written only when <see cref="RewardItem"/> is present.</summary>
    [Id(12)]
    public CollectibleItemClaimSnapshot? RewardItemClaim { get; init; }
}
