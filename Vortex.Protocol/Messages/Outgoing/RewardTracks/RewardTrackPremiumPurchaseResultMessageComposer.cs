using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.RewardTracks;

namespace Vortex.Protocol.Messages.Outgoing.RewardTracks;

/// <summary>
/// The answer to a premium purchase. On success the client marks the track premium and takes
/// <see cref="Points"/> as the new total, which is how the instant-points bonus reaches it.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RewardTrackPremiumPurchaseResultMessageComposer : IComposer
{
    [Id(0)]
    public required string TrackId { get; init; }

    [Id(1)]
    public required RewardPremiumResult Result { get; init; }

    /// <summary>The track's point total after the purchase. Ignored by the client on a failure.</summary>
    [Id(2)]
    public required int Points { get; init; }
}
