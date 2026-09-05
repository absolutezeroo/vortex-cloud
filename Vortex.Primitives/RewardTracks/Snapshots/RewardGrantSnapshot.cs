using Orleans;

namespace Vortex.Primitives.RewardTracks.Snapshots;

/// <summary>One thing a prize hands over. A prize may hand over several.</summary>
[GenerateSerializer, Immutable]
public sealed record RewardGrantSnapshot
{
    [Id(0)]
    public required RewardKind Kind { get; init; }

    /// <summary>
    /// What the kind names: a furniture id, a badge code, an activity-point type, an entitlement
    /// key. Kept as a string because that is the client's own field type and half the kinds are not
    /// numbers.
    /// </summary>
    [Id(1)]
    public required string RewardTypeId { get; init; }

    /// <summary>How many. Currency amount, item quantity, 1 for anything singular.</summary>
    [Id(2)]
    public required int Amount { get; init; }

    /// <summary>Figure strings for bots and pets, extra data for furniture. Empty otherwise.</summary>
    [Id(3)]
    public required string ExtraParams { get; init; }
}
