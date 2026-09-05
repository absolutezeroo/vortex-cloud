using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.RewardTracks;

namespace Vortex.Protocol.Messages.Outgoing.RewardTracks;

/// <summary>
/// The answer to a claim, successful or not. A failure is answered as loudly as a success: the
/// client shows the localized reason and, without this, would sit on a spinning button.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RewardTrackClaimResultMessageComposer : IComposer
{
    [Id(0)]
    public required string TrackId { get; init; }

    [Id(1)]
    public required string PrizeId { get; init; }

    [Id(2)]
    public required RewardClaimResult Result { get; init; }
}
