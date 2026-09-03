using Orleans;

namespace Vortex.Primitives.Sound.Snapshots;

/// <summary>
/// A song disk: the furniture item, and the song pressed on it.
/// </summary>
/// <remarks>
/// The client keys its disk inventory and every jukebox playlist by <see cref="DiskId" /> and looks
/// the song up by <see cref="SongId" />, which is why both travel together everywhere. They are two
/// different things and neither substitutes for the other: several disks can carry the same song.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record SongDiskSnapshot
{
    /// <summary>The furniture item id.</summary>
    [Id(0)]
    public required int DiskId { get; init; }

    [Id(1)]
    public required int SongId { get; init; }
}
