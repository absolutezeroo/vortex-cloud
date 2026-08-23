using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Inventory.Trading;

/// <summary>
/// How much silver each side has staked towards the trade fee (header 1490).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_2546/_SafeCls_2855.as): two ints, the recipient's
/// own stake first.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record TradeSilverSetMessageComposer : IComposer
{
    /// <summary>The stake of the player this packet is being sent to.</summary>
    [Id(0)]
    public required int PlayerSilver { get; init; }

    [Id(1)]
    public required int OtherPlayerSilver { get; init; }
}
