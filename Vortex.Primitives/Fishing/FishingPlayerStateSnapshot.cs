using Orleans;

namespace Vortex.Primitives.Fishing;

/// <summary>
/// Where one player stands in the fishing skill, as the client is told about it.
/// </summary>
/// <remarks>
/// Vortex-specific: no AS3 or Habbo equivalent. The contract is the client's own
/// <c>VortexFishingPlayerStateMessageParser</c>; see that repository's
/// <c>docs/vortex-original/fishing.md</c>.
///
/// <para>The client computes none of this. It is pushed after every catch, which is also what keeps
/// the records tab and the level bar honest without polling.</para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record FishingPlayerStateSnapshot
{
    /// <summary>Unlocks zones. Separate from the rod.</summary>
    [Id(0)]
    public required int FishingLevel { get; init; }

    [Id(1)]
    public required int FishingXp { get; init; }

    /// <summary>Rod quality tier — multipliers and the Hook Havoc chance. Not the level.</summary>
    [Id(2)]
    public required int RodQuality { get; init; }

    [Id(3)]
    public required int RodXp { get; init; }

    [Id(4)]
    public required int Currency { get; init; }

    /// <summary>Already reset to zero by the reader when the stored date is not today.</summary>
    [Id(5)]
    public required int CurrencyEarnedToday { get; init; }

    /// <summary>Zero means uncapped.</summary>
    [Id(6)]
    public required int DailyCap { get; init; }

    /// <summary>Catches in the session running now, or zero when none is. Drives the decay display.</summary>
    [Id(7)]
    public required int SessionCatchCount { get; init; }

    /// <summary>Bottles, statues and the badge this player holds.</summary>
    [Id(8)]
    public required int[] CollectibleIds { get; init; }

    [Id(9)]
    public required int TotalCatches { get; init; }

    [Id(10)]
    public required int GoldenCatches { get; init; }
}
