using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Sound.Admin;

namespace Vortex.Primitives.Sound;

/// <summary>
/// The hotel's song catalogue, as an operator edits it.
/// </summary>
/// <remarks>
/// Without this the <c>songs</c> table can only be filled by hand in SQL, and a hotel with no rows
/// in it has a jukebox that loads disks and plays nothing — the client will not play a song it
/// cannot name. Every write here reloads <see cref="Providers.ISongProvider" />, so an added or
/// corrected song is live without an emulator restart.
/// </remarks>
public interface ISongAdminService
{
    Task<SongAdminResult> CreateAsync(SongSpec spec, CancellationToken ct);

    Task<SongAdminResult> UpdateAsync(int songId, SongSpec spec, CancellationToken ct);

    /// <summary>
    /// Soft-deletes a song. The disks pressed with it keep their id and simply stop resolving, which
    /// is the same state as a disk for a song this hotel never had — recoverable by restoring the
    /// row, which a hard delete would not be.
    /// </summary>
    Task<SongAdminResult> DeleteAsync(int songId, CancellationToken ct);

    /// <summary>Rebuilds the live catalogue from the database without an emulator restart.</summary>
    Task<SongAdminResult> ReloadAsync(CancellationToken ct);
}
