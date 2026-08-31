using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Fishing;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Fishing;

/// <summary>
/// Every fishing definition table, in one message. Vortex-specific: no AS3 or Habbo equivalent.
/// </summary>
/// <remarks>
/// <strong>Re-sendable at any time.</strong> This is not a login-only fetch: an operator editing a
/// catch rate in the dashboard bumps <see cref="Version"/> and this is broadcast to every connected
/// session, so a player already standing at a pond sees the change without reconnecting. The client
/// ignores a push whose version is not newer, which makes a redundant re-broadcast free.
///
/// <para>That is the whole reason the tables travel as a packet instead of as a gamedata file: a
/// file is fetched at boot and needs a page reload to change.</para>
///
/// <para>Field order is the contract with vortex-modern-client's
/// <c>VortexFishingDefinitionsMessageParser</c> — keep the two in lockstep, and only ever append.
/// See that repository's <c>docs/vortex-original/fishing.md</c>.</para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record VortexFishingDefinitionsMessageComposer : IComposer
{
    /// <summary>Bumped on every reload. The client drops a push that is not newer than what it has.</summary>
    [Id(0)]
    public required int Version { get; init; }

    [Id(1)]
    public required IReadOnlyList<FishSpeciesSnapshot> Species { get; init; }

    /// <summary>Rod quality tiers — multipliers and Hook Havoc chance.</summary>
    [Id(2)]
    public required IReadOnlyList<FishingRodLevelSnapshot> RodLevels { get; init; }

    /// <summary>The fishing level curve, which unlocks zones. A separate progression from the rod.</summary>
    [Id(4)]
    public required IReadOnlyList<FishingLevelSnapshot> FishingLevels { get; init; }

    [Id(3)]
    public required IReadOnlyList<FishingZoneSnapshot> Zones { get; init; }
}
