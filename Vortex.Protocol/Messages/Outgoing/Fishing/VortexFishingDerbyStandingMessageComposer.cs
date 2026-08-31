using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Fishing;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Fishing;

/// <summary>
/// The derby leaderboard as it stands. Vortex-specific: no AS3 or Habbo equivalent.
/// </summary>
/// <remarks>
/// <strong>Vortex's own addition, not an Origins feature.</strong> Origins has the Fishing Frenzy —
/// a schedule, not a leaderboard. The derby is the contest this hotel asked for.
///
/// <para><see cref="OwnRank"/> is sent separately from the entries because a player outside the
/// visible top still has to be told where they are, and sending the whole board to find out would
/// grow with the hotel.</para>
///
/// <para>Field order is the contract with vortex-modern-client's
/// <c>VortexFishingDerbyStandingMessageParser</c>. Append-only.</para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record VortexFishingDerbyStandingMessageComposer : IComposer
{
    [Id(0)]
    public required int DerbyId { get; init; }

    /// <summary>Unix seconds.</summary>
    [Id(1)]
    public required int EndsAt { get; init; }

    /// <summary>Ordered best-first, already truncated to what the client shows.</summary>
    [Id(2)]
    public required IReadOnlyList<FishingDerbyEntrySnapshot> Entries { get; init; }

    /// <summary>Counted from 1. Zero when the player has not joined this derby.</summary>
    [Id(3)]
    public required int OwnRank { get; init; }
}
