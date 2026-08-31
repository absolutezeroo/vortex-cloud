using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Fishing;

/// <summary>
/// How a Hook Havoc attempt ended, as the server replayed it. Vortex-specific: no AS3 or Habbo
/// equivalent.
/// </summary>
/// <remarks>
/// Failure costs nothing in Origins and fishing resumes immediately, so this is not an error
/// message: <see cref="Won"/> false is an ordinary outcome with zero rewards.
///
/// <para>Field order is the contract with vortex-modern-client's
/// <c>VortexHookHavocResultMessageParser</c>. Append-only.</para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record VortexHookHavocResultMessageComposer : IComposer
{
    [Id(0)]
    public required int AttemptId { get; init; }

    [Id(1)]
    public required bool Won { get; init; }

    /// <summary>The Golden Fish caught. Zero on a loss.</summary>
    [Id(2)]
    public required int SpeciesId { get; init; }

    [Id(3)]
    public required int XpGained { get; init; }

    [Id(4)]
    public required int CurrencyGained { get; init; }

    /// <summary>
    /// The trophy that hangs visibly from the rod. A carry-object id at or above 1000, which is
    /// above the client's <c>CARRY_ITEM_LAST_CONSUMABLE</c> — below it the avatar would drink it.
    /// Zero on a loss.
    /// </summary>
    [Id(5)]
    public required int TrophyHandItemId { get; init; }
}
