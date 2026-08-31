using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Fishing;

/// <summary>
/// Every fishing definition table, and the version that says whether they have changed.
/// </summary>
/// <remarks>
/// The version is what makes a redundant broadcast free: a client drops a push that is not newer
/// than what it already holds, so re-sending on every reconnect costs nothing and an operator's
/// edit still reaches a player mid-session.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record FishingDefinitionsSnapshot
{
    [Id(0)]
    public required int Version { get; init; }

    [Id(1)]
    public required ImmutableArray<FishSpeciesSnapshot> Species { get; init; }

    /// <summary>Rod quality tiers — multipliers and Hook Havoc chance.</summary>
    [Id(2)]
    public required ImmutableArray<FishingRodLevelSnapshot> RodTiers { get; init; }

    /// <summary>The fishing level curve, which unlocks zones. A separate progression from the rod.</summary>
    [Id(3)]
    public required ImmutableArray<FishingLevelSnapshot> Levels { get; init; }

    [Id(4)]
    public required ImmutableArray<FishingZoneSnapshot> Zones { get; init; }

    /// <summary>
    /// The tunables, read by the same reload. Deliberately not serialized to the client: only the
    /// daily cap concerns it, and that travels in the player-state message.
    /// </summary>
    [Id(5)]
    public required FishingSettingsSnapshot Settings { get; init; }
}
