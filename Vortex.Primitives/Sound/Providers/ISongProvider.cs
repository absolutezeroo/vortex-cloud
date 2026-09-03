using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Sound.Snapshots;

namespace Vortex.Primitives.Sound.Providers;

/// <summary>
/// The hotel's songs, held in memory: read on every song disk the client sees and every jukebox
/// playlist it draws, written only when an operator reloads reference data.
/// </summary>
public interface ISongProvider
{
    public SongSnapshot? TryGetSong(int id);

    /// <summary>
    /// Resolves the external code a catalogue song-disk offer carries in its <c>extraParam</c> to
    /// the numeric id the rest of the protocol speaks. Null when this hotel does not ship that song.
    /// </summary>
    public SongSnapshot? TryGetSongByOfficialId(string officialSongId);

    public Task ReloadAsync(CancellationToken ct);
}
