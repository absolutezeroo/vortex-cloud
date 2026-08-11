using Orleans;

namespace Vortex.Primitives.Players.Snapshots;

[GenerateSerializer, Immutable]
public sealed record PlayerBadgeSnapshot
{
    [Id(0)]
    public required int SlotId { get; init; }

    [Id(1)]
    public required string BadgeCode { get; init; }

    /// <summary>How many players hold this badge. Read by the client as the third field of every
    /// badge in <c>BadgesEventMessageComposer</c> (WIN63 unknowns/_SafePkg_3206/_SafeCls_3564.as,
    /// which builds a Badge from slotId/badgeCode/ownerCount/badgeRarityId).</summary>
    [Id(2)]
    public int OwnerCount { get; init; }

    /// <summary>The badge's rarity tier, fourth and last field of every badge on the wire. Drives
    /// the client's rarity overlay; 0 means "no tier".</summary>
    [Id(3)]
    public int BadgeRarityId { get; init; }
}
