using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Fishing;

/// <summary>
/// Where this player stands in the fishing skill. Vortex-specific: no AS3 or Habbo equivalent.
/// </summary>
/// <remarks>
/// Pushed at login and after every catch. The client computes none of it — the two levels, the two
/// XP counters, the daily cap and the session count are all decided here — so this is the one
/// message that says where a player is, and it is what keeps the records tab and the level bar
/// honest without polling.
///
/// <para>Field order is the contract with vortex-modern-client's
/// <c>VortexFishingPlayerStateMessageParser</c>. Append-only.</para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record VortexFishingPlayerStateMessageComposer : IComposer
{
    [Id(0)]
    public required int FishingLevel { get; init; }

    [Id(1)]
    public required int FishingXp { get; init; }

    /// <summary>Rod quality tier. A separate progression from the level.</summary>
    [Id(2)]
    public required int RodQuality { get; init; }

    [Id(3)]
    public required int RodXp { get; init; }

    [Id(4)]
    public required int Currency { get; init; }

    [Id(5)]
    public required int CurrencyEarnedToday { get; init; }

    /// <summary>Zero means uncapped; the client reads it that way too.</summary>
    [Id(6)]
    public required int DailyCap { get; init; }

    [Id(7)]
    public required int SessionCatchCount { get; init; }

    [Id(8)]
    public required IReadOnlyList<int> CollectibleIds { get; init; }
}
