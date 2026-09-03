using Orleans;

namespace Vortex.Primitives.Sound.Snapshots;

/// <summary>Why a disk did or did not move.</summary>
public enum JukeboxLoadOutcome
{
    /// <summary>The disk moved; <see cref="JukeboxLoadResult.Playlist" /> is what to send out.</summary>
    Moved,

    /// <summary>Nothing to load into, no rights to it, or the disk was not the caller's to give.</summary>
    Refused,

    /// <summary>The jukebox is at capacity. The client has its own dialog for this one.</summary>
    Full,
}

[GenerateSerializer, Immutable]
public sealed record JukeboxLoadResult
{
    [Id(0)]
    public required JukeboxLoadOutcome Outcome { get; init; }

    [Id(1)]
    public required JukeboxPlaylistSnapshot Playlist { get; init; }

    public static readonly JukeboxLoadResult Refused = new()
    {
        Outcome = JukeboxLoadOutcome.Refused,
        Playlist = JukeboxPlaylistSnapshot.Empty,
    };
}
