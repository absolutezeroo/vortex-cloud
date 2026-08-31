using Orleans;

namespace Vortex.Primitives.Fishing;

/// <summary>
/// What one catch actually granted, after the caps and the curves were applied.
/// </summary>
/// <remarks>
/// <para>
/// Vortex-specific: no AS3 or Habbo equivalent. See the client's
/// <c>docs/vortex-original/fishing.md</c>.
/// </para>
/// <para>
/// The session grain rolls the fish and proposes the rewards; the player grain is what decides what
/// the player <em>keeps</em>, because only it knows the balance, the day's total and the two XP
/// curves. So the granted amounts here can be lower than the proposed ones — the daily cap truncates
/// currency — and they, not the proposal, are what the client is told.
/// </para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record FishingCatchOutcome
{
    /// <summary>What <c>MountCatch</c> later names to mint a trophy. Zero when nothing was recorded.</summary>
    [Id(0)]
    public required int RecordId { get; init; }

    [Id(1)]
    public required int XpGranted { get; init; }

    /// <summary>Already truncated by the daily cap, so this is what to display.</summary>
    [Id(2)]
    public required int CurrencyGranted { get; init; }

    /// <summary>The new fishing level, or zero when the catch did not raise it.</summary>
    [Id(3)]
    public required int NewFishingLevel { get; init; }

    /// <summary>The new rod quality, or zero when the catch did not raise it.</summary>
    [Id(4)]
    public required int NewRodQuality { get; init; }

    /// <summary>
    /// True once the day's currency is spent. The session stops on it rather than fishing on for
    /// nothing — the client greys the panel out for the same reason.
    /// </summary>
    [Id(5)]
    public required bool DailyCapReached { get; init; }
}
