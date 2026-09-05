using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.RewardTracks.Snapshots;

/// <summary>
/// A milestone: reach these points and this becomes claimable. A prize knows nothing about which
/// task paid for the points.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RewardTrackPrizeDefinitionSnapshot
{
    /// <summary>
    /// Content id, unique within the track, and the identity a claim is recorded against. Stable
    /// across content edits on purpose: rewriting what <c>intro_lvl_5</c> hands out must not make an
    /// old claim ambiguous.
    /// </summary>
    [Id(0)]
    public required string PrizeId { get; init; }

    [Id(1)]
    public required int RequiredPoints { get; init; }

    /// <summary>Claimable only with premium on this track.</summary>
    [Id(2)]
    public required bool Premium { get; init; }

    [Id(3)]
    public required int SortOrder { get; init; }

    /// <summary>
    /// Everything this prize hands over. The client can only draw one, so the first is what it
    /// shows; the rest are granted all the same.
    /// </summary>
    [Id(4)]
    public required ImmutableArray<RewardGrantSnapshot> Rewards { get; init; }

    /// <summary>What the client draws for this prize. Never empty for a valid prize.</summary>
    public RewardGrantSnapshot? Display => Rewards.IsDefaultOrEmpty ? null : Rewards[0];
}
