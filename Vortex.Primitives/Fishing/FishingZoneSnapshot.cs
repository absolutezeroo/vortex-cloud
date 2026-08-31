using Orleans;

namespace Vortex.Primitives.Fishing;

/// <summary>
/// One fishing zone — a spot furni class, plus the level needed to fish it.
/// </summary>
/// <remarks>
/// Vortex-specific: no AS3 or Habbo equivalent. See the client's
/// <c>docs/vortex-original/fishing.md</c>.
///
/// <para>Keyed by furni class rather than by item id: every copy of a spot behaves the same, and a
/// room owner placing a second one changes nothing.</para>
///
/// <para><strong>A spot depletes.</strong> Origins runs fishing as a session: the player clicks a
/// fish shadow, the avatar fishes on its own, and the spot runs dry after an unpredictable number of
/// catches — "one fish or several" — at which point the player relocates. The bounds below are what
/// the server rolls a fresh spot's stock between.</para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record FishingZoneSnapshot
{
    [Id(0)]
    public required int Id { get; init; }

    /// <summary>A localisation key, never a display string.</summary>
    [Id(1)]
    public required string NameKey { get; init; }

    [Id(2)]
    public required string FurniClass { get; init; }

    /// <summary>Zero means everybody.</summary>
    [Id(3)]
    public required int RequiredLevel { get; init; }

    /// <summary>Fewest catches a fresh spot yields before running dry.</summary>
    [Id(4)]
    public required int MinCatches { get; init; }

    /// <summary>Most catches a fresh spot yields. Equal to <see cref="MinCatches"/> for a fixed stock.</summary>
    [Id(5)]
    public required int MaxCatches { get; init; }
}
