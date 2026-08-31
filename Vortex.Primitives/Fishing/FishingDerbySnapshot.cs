using System.Collections.Immutable;
using Orleans;

namespace Vortex.Primitives.Fishing;

/// <summary>
/// A fishing derby: a time-boxed leaderboard scored on the heaviest single catch.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Vortex's own, not Origins'.</strong> Origins has a Fishing Frenzy — every four hours,
/// every catch triggers Hook Havoc and XP is ×5 — but no leaderboard contest. The derby is the
/// "concours" this hotel asked for, kept as an addition rather than passed off as a reconstruction.
/// See the client's <c>docs/vortex-original/fishing.md</c> for which parts are which.
/// </para>
/// <para>
/// Heaviest single catch rather than a total, because a total rewards whoever left the client open
/// longest, and fishing here is unattended by design.
/// </para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record FishingDerbySnapshot
{
    [Id(0)]
    public required int Id { get; init; }

    /// <summary>Unix seconds. Zero when no derby is scheduled, which is the ordinary state.</summary>
    [Id(1)]
    public required int EndsAt { get; init; }

    /// <summary>Ordered best-first, and already truncated to what the client shows.</summary>
    [Id(2)]
    public required ImmutableArray<FishingDerbyEntrySnapshot> Entries { get; init; }
}

/// <summary>One line of a derby leaderboard.</summary>
[GenerateSerializer, Immutable]
public sealed record FishingDerbyEntrySnapshot
{
    [Id(0)]
    public required int PlayerId { get; init; }

    [Id(1)]
    public required string PlayerName { get; init; }

    /// <summary>The heaviest catch this player has landed during the derby.</summary>
    [Id(2)]
    public required int Score { get; init; }
}
