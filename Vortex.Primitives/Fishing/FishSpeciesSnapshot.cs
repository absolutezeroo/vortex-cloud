using Orleans;

namespace Vortex.Primitives.Fishing;

/// <summary>
/// One fish species, as the client is told about it.
/// </summary>
/// <remarks>
/// Vortex-specific: no AS3 or Habbo equivalent, so there is no spec to consult and no reference
/// emulator to corroborate. The contract is the client's own
/// <c>VortexFishingDefinitionsMessageParser</c>, and the design is in that repository under
/// <c>docs/vortex-original/fishing.md</c>.
///
/// <para><see cref="CatchRate"/> is the whole difficulty model. There is no minigame, so there is no
/// skill to erase rarity: a rare fish is rare because it seldom appears and often escapes, and
/// practice changes neither. Nothing else about a species makes it hard.</para>
///
/// <para>Weights are integers in the simulation's own unit — the wire carries no fractional
/// values.</para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record FishSpeciesSnapshot
{
    [Id(0)]
    public required int Id { get; init; }

    /// <summary>A localisation key, never a display string.</summary>
    [Id(1)]
    public required string NameKey { get; init; }

    [Id(2)]
    public required int ZoneId { get; init; }

    /// <summary>Below this fishing level the species is not in the zone's table at all.</summary>
    [Id(3)]
    public required int RequiredLevel { get; init; }

    /// <summary>1-5, for display only. The number that decides anything is <see cref="CatchRate"/>.</summary>
    [Id(4)]
    public required int RarityStars { get; init; }

    /// <summary>Tenths of a percent, so 850 is 85%.</summary>
    [Id(5)]
    public required int CatchRate { get; init; }

    /// <summary>Relative weight when picking which species swims past.</summary>
    [Id(6)]
    public required int RarityWeight { get; init; }

    [Id(7)]
    public required int MinWeight { get; init; }

    [Id(8)]
    public required int MaxWeight { get; init; }

    [Id(9)]
    public required int XpReward { get; init; }

    [Id(10)]
    public required int GoldenXpBonus { get; init; }

    [Id(11)]
    public required int CurrencyReward { get; init; }

    /// <summary>24-bit mask, bit h set means available during hour h UTC — nocturnal species.</summary>
    [Id(12)]
    public required int ActiveHours { get; init; }

    /// <summary>7-bit mask, bit 0 is Sunday.</summary>
    [Id(13)]
    public required int ActiveWeekdays { get; init; }

    /// <summary>
    /// Season mask — the fourth availability axis, alongside hour, weekday and zone. Origins' guides
    /// name "seasonal events" as one; <strong>how Origins encodes a season is unknown</strong>, and
    /// the four-bit reading here is a guess. See the client's docs/vortex-original/fishing.md.
    /// </summary>
    [Id(14)]
    public required int ActiveSeasons { get; init; }
}
