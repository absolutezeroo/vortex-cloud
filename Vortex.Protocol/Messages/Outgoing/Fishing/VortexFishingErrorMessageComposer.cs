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
}
