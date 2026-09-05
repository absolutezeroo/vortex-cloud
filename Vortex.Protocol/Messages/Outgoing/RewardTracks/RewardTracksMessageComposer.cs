using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.RewardTracks.Snapshots;

namespace Vortex.Protocol.Messages.Outgoing.RewardTracks;

/// <summary>
/// Every reward track this player can see, resolved against their own progress. There is no request
/// for it: the client builds its whole model from whatever the server pushes, so this goes out at
/// login and again whenever the answer changes.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RewardTracksMessageComposer : IComposer
{
    /// <summary>
    /// Turns the feature off client-side: it drops every track and shows nothing. Sent true when
    /// the hotel has reward tracks disabled, which is not the same as having none.
    /// </summary>
    [Id(0)]
    public required bool Disabled { get; init; }

    [Id(1)]
    public required ImmutableArray<RewardTrackViewSnapshot> Tracks { get; init; }

    /// <summary>
    /// Tells the client the content itself changed: it throws away its cached windows and, if one
    /// was open, says so ("Reward Track updated"). Set after an operator edits a published track,
    /// never after ordinary progress.
    /// </summary>
    [Id(2)]
    public required bool Reload { get; init; }
}
