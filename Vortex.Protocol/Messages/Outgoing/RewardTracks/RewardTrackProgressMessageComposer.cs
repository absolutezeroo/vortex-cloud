using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.RewardTracks;

/// <summary>
/// One task advanced. Carries the track's new total as well, because a stage completing pays points
/// and the client would otherwise have to guess how many.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RewardTrackProgressMessageComposer : IComposer
{
    [Id(0)]
    public required string TrackId { get; init; }

    [Id(1)]
    public required string TaskId { get; init; }

    [Id(2)]
    public required int ProgressCount { get; init; }

    /// <summary>The track's point total after this update, not the points this update paid.</summary>
    [Id(3)]
    public required int Points { get; init; }
}
