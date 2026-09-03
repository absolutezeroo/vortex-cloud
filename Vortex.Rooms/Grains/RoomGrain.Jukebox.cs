using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Sound.Snapshots;

namespace Vortex.Rooms.Grains;

/// <summary>
/// The jukebox facet: everything here is the room's turn, which is what makes two players loading a
/// disk at the same moment two sequential moves rather than one race.
/// </summary>
public sealed partial class RoomGrain
{
    public Task<JukeboxPlaylistSnapshot> GetPlaylistAsync(CancellationToken ct) =>
        JukeboxSystem.GetPlaylistAsync(ct);

    public Task<JukeboxLoadResult> AddDiskAsync(
        ActionContext ctx,
        int diskItemId,
        CancellationToken ct
    ) => JukeboxSystem.AddDiskAsync(ctx, diskItemId, ct);

    public Task<JukeboxLoadResult> RemoveDiskAsync(
        ActionContext ctx,
        int index,
        CancellationToken ct
    ) => JukeboxSystem.RemoveDiskAsync(ctx, index, ct);

    public Task<NowPlayingSnapshot> GetNowPlayingAsync(CancellationToken ct) =>
        JukeboxSystem.GetNowPlayingAsync(ct);
}
