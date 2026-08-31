using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Fishing;

/// <summary>
/// Why a fishing request was refused. Vortex-specific: no AS3 or Habbo equivalent.
/// </summary>
/// <remarks>
/// For a request that should not have been made. A fish that simply escaped is not an error and
/// never arrives as one — it is the ordinary outcome of a catch roll.
///
/// <para>An integer rather than a string so the message stays cheap; the client derives the
/// localisation key from the code. Codes are append-only on both sides — see
/// <c>Vortex.Primitives.Fishing.FishingErrorCode</c>.</para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record VortexFishingErrorMessageComposer : IComposer
{
    /// <summary>A <c>FishingErrorCode</c>, sent raw so an older client still parses an unknown one.</summary>
    [Id(0)]
    public required int Code { get; init; }

    /// <summary>
    /// The one number the code needs to be actionable, or zero when it needs none.
    /// </summary>
    /// <remarks>
    /// Its meaning is the code's: for <c>LevelTooLow</c> it is the zone's required level, which is
    /// the whole difference between "your level is too low" and a message a player can act on. The
    /// client cannot work it out — it is refused before it ever learns which zone the spot is in —
    /// and this is the exact moment the server compared the two.
    ///
    /// <para>One shared field rather than a per-code payload: every code that has ever wanted
    /// context has wanted a single integer, and a union would cost a length prefix on every
    /// refusal that wants nothing.</para>
    /// </remarks>
    [Id(1)]
    public int Detail { get; init; }
}
