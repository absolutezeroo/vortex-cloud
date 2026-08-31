using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Fishing;

/// <summary>
/// A catch landed. Vortex-specific: no AS3 or Habbo equivalent.
/// </summary>
/// <remarks>
/// Everything here was decided before the message was written: the species, the weight, the roll
/// against the catch rate and both rewards. The client displays it and files <see cref="RecordId"/>,
/// which is what <c>MountCatch</c> later refers to.
///
/// <para>A <c>FishingPlayerState</c> push follows carrying the new totals. This message deliberately
/// does not restate them, so a balance or a level has exactly one source.</para>
///
/// <para>Field order is the contract with vortex-modern-client's
/// <c>VortexFishingCatchResultMessageParser</c>. Append-only.</para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record VortexFishingCatchResultMessageComposer : IComposer
{
    [Id(0)]
    public required int RecordId { get; init; }

    [Id(1)]
    public required int SpeciesId { get; init; }

    [Id(2)]
    public required int Weight { get; init; }

    /// <summary>Already multiplied by the rod tier and the frenzy, so it is what to show.</summary>
    [Id(3)]
    public required int XpGained { get; init; }

    /// <summary>Already truncated by the daily cap.</summary>
    [Id(4)]
    public required int CurrencyGained { get; init; }

    [Id(5)]
    public required bool Golden { get; init; }

    /// <summary>Zero when the catch did not level the player up.</summary>
    [Id(6)]
    public required int NewLevel { get; init; }
}
