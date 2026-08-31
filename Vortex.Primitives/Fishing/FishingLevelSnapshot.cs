using Orleans;

namespace Vortex.Primitives.Fishing;

/// <summary>
/// One fishing level — the progression that unlocks zones.
/// </summary>
/// <remarks>
/// Reconstructed from Habbo Origins; see the client's <c>docs/vortex-original/fishing.md</c> for how
/// well any of it is known.
///
/// <para><strong>Separate from the rod.</strong> The fishing level unlocks zones and nothing else
/// observed; reward size lives on <see cref="FishingRodLevelSnapshot"/> instead. The curve's real
/// numbers are unknown.</para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record FishingLevelSnapshot
{
    [Id(0)]
    public required int Level { get; init; }

    /// <summary>Cumulative <em>fishing</em> XP at which this level begins.</summary>
    [Id(1)]
    public required int XpThreshold { get; init; }
}
