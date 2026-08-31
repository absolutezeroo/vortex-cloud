using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Fishing;

/// <summary>
/// The spot has run dry and the session is over. Vortex-specific: no AS3 or Habbo equivalent.
/// </summary>
/// <remarks>
/// The Origins loop is start-once, run-until-dry, relocate: a spot yields "one fish or several" and
/// then the player has to move to another shadow. This is the end of the stream — no further
/// sighting arrives for that spot without a new <c>StartFishing</c>.
///
/// <para>Field order is the contract with vortex-modern-client's
/// <c>VortexFishingSpotDepletedMessageParser</c>. Append-only.</para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record VortexFishingSpotDepletedMessageComposer : IComposer
{
    [Id(0)]
    public required int SpotItemId { get; init; }

    /// <summary>How many catches the session produced — what the client reports in the panel.</summary>
    [Id(1)]
    public required int Catches { get; init; }
}
