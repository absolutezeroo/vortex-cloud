namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One song, with how many disks of it exist and how many are sitting in a jukebox.
/// </summary>
/// <param name="LengthSeconds">
/// The stored milliseconds rounded for reading. Both are sent: the page shows one and sorts on
/// the other.
/// </param>
/// <param name="DiskCount">Disks minted for this song, wherever they are.</param>
/// <param name="LoadedInJukeboxes">How many of those are loaded into a jukebox.</param>
public sealed record SongListItem(
    int Id,
    string Name,
    string Creator,
    int LengthMs,
    double LengthSeconds,
    string OfficialSongId,
    string Data,
    int DiskCount,
    int LoadedInJukeboxes
);
