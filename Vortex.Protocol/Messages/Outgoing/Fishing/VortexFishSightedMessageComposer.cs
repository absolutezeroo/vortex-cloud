using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Fishing;

/// <summary>
/// A fish is passing the spot. Vortex-specific: no AS3 or Habbo equivalent.
/// </summary>
/// <remarks>
/// The species is deliberately absent. A sighting is a shadow in the water: naming the fish here
/// would let a client filter for rare ones before the catch resolves, and the catch is resolved
/// server-side precisely so that it cannot. <c>CatchResult</c> is the first and only time a species
/// is named.
///
/// <para><c>Golden</c> is the exception, because a Golden Fish is visible in the water in Origins
/// and the client has to draw the shadow differently.</para>
///
/// <para>Field order is the contract with vortex-modern-client's
/// <c>VortexFishSightedMessageParser</c>. Append-only.</para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record VortexFishSightedMessageComposer : IComposer
{
    /// <summary>Server-issued, and the handle a catch is logged against.</summary>
    [Id(0)]
    public required int SightingId { get; init; }

    [Id(1)]
    public required int SpotItemId { get; init; }

    [Id(2)]
    public required bool Golden { get; init; }

    /// <summary>How long the shadow is drawn before the catch resolves.</summary>
    [Id(3)]
    public required int DurationMs { get; init; }
}
