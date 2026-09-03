using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Sound.Snapshots;

namespace Vortex.Primitives.Rooms.Grains;

/// <summary>The jukebox standing in the room, and the disks loaded into it.</summary>
/// <remarks>
/// The playlist belongs to the jukebox furniture, not to the room — the client's requests carry no
/// identifier at all, so the room resolves its own jukebox — and that is what lets a playlist
/// survive the owner picking the jukebox up and putting it down again.
/// </remarks>
[Alias("Vortex.Primitives.Rooms.Grains.IRoomJukebox")]
public interface IRoomJukebox : IGrainWithIntegerKey
{
    /// <summary>The disks currently loaded, in play order.</summary>
    public Task<JukeboxPlaylistSnapshot> GetPlaylistAsync(CancellationToken ct);

    /// <summary>
    /// Loads one of the caller's song disks into the room's jukebox.
    /// </summary>
    /// <returns>
    /// The playlist as it now stands, or null when nothing moved — no jukebox in the room, no rights
    /// to it, or the disk was not the caller's to give. A full jukebox is
    /// <see cref="JukeboxLoadOutcome.Full" />, which the client answers with its own dialog.
    /// </returns>
    public Task<JukeboxLoadResult> AddDiskAsync(
        ActionContext ctx,
        int diskItemId,
        CancellationToken ct
    );

    /// <summary>
    /// Takes the disk at <paramref name="index" /> back out and returns it to its owner's hands.
    /// </summary>
    /// <remarks>
    /// The client identifies the disk by its position in the list it was last sent, which is the
    /// only handle it has. A stale index is a no-op rather than a guess at what was meant.
    /// </remarks>
    public Task<JukeboxLoadResult> RemoveDiskAsync(
        ActionContext ctx,
        int index,
        CancellationToken ct
    );

    /// <summary>Where the playlist is right now, for a client that just walked in.</summary>
    public Task<NowPlayingSnapshot> GetNowPlayingAsync(CancellationToken ct);
}
