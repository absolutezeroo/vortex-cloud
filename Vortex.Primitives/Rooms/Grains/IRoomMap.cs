using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Action;
using Vortex.Primitives.Rooms.Snapshots.Mapping;

namespace Vortex.Primitives.Rooms.Grains;

/// <summary>The room's tile map: tile queries and tile clicks.</summary>
[Alias("Vortex.Primitives.Rooms.Grains.IRoomMap")]
public interface IRoomMap : IGrainWithIntegerKey
{
    public Task ClickTileAsync(ActionContext ctx, int x, int y, CancellationToken ct);
    public Task<RoomTileSnapshot> GetTileSnapshotAsync(int x, int y, CancellationToken ct);
    public Task<RoomTileSnapshot> GetTileSnapshotAsync(int id, CancellationToken ct);
    public Task<RoomMapSnapshot> GetMapSnapshotAsync(CancellationToken ct);

    /// <summary>
    /// Every tile with furniture standing on it, for the floor-plan editor. Deliberately not
    /// <c>[ReadOnly]</c>: it walks the live flag array, which the tick rewrites, and an interleaved
    /// read would see a half-updated map.
    /// </summary>
    public Task<ImmutableArray<(int X, int Y)>> GetOccupiedTilesAsync(CancellationToken ct);
}
