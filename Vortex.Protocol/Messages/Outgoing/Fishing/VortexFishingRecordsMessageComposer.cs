using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Fishing;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Fishing;

/// <summary>
/// The player's Fishopedia. Vortex-specific: no AS3 or Habbo equivalent.
/// </summary>
/// <remarks>
/// Only caught species appear, so the book shows an entry as undiscovered by its absence rather than
/// by a flag — which keeps the message proportional to what the player has done rather than to how
/// many species the operator has defined.
///
/// <para>Field order is the contract with vortex-modern-client's
/// <c>VortexFishingRecordsMessageParser</c>. Append-only.</para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record VortexFishingRecordsMessageComposer : IComposer
{
    [Id(0)]
    public required IReadOnlyList<FishingRecordSnapshot> Records { get; init; }
}
