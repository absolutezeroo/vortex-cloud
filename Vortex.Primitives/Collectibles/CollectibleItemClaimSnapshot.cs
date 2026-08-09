using Orleans;

namespace Vortex.Primitives.Collectibles;

/// <summary>Whether a collection's bonus or reward has been taken, and how often it may be.</summary>
[GenerateSerializer, Immutable]
public sealed record CollectibleItemClaimSnapshot
{
    [Id(0)]
    public required string ClaimId { get; init; }

    [Id(1)]
    public int ClaimedAmount { get; init; }

    /// <summary>How many times this may be claimed at all; one for an ordinary reward.</summary>
    [Id(2)]
    public int ClaimLimit { get; init; } = 1;

    /// <summary>A short on the wire. See <see cref="CollectibleClaimStatus"/>.</summary>
    [Id(3)]
    public int Status { get; init; }
}

/// <summary>
/// The states the client draws a claim in. Named here rather than left as bare numbers, since the
/// difference between "claimable" and "claimed" is the whole point of the button.
/// </summary>
public static class CollectibleClaimStatus
{
    /// <summary>The collection is not finished, so there is nothing to take yet.</summary>
    public const int NotClaimable = 0;

    public const int Claimable = 1;

    public const int Claimed = 2;
}
