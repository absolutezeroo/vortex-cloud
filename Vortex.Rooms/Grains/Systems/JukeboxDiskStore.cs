using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// The two statements that move a song disk in and out of a jukebox, and the one that reads a
/// playlist.
/// </summary>
/// <remarks>
/// Separate from <see cref="RoomJukeboxSystem" /> because this is the part where an item can go
/// wrong, and it is worth being able to run it against a real database without standing up a room.
/// Both moves are a single <c>UPDATE</c> whose <c>WHERE</c> carries the entire precondition, so a
/// replayed request updates zero rows rather than moving the disk a second time, and there is no
/// moment at which the disk is in two places or in none.
/// </remarks>
internal static class JukeboxDiskStore
{
    /// <summary>
    /// Loads one of <paramref name="playerId" />'s disks into <paramref name="jukeboxId" />.
    /// </summary>
    /// <returns>1 when the disk moved, 0 when it was not the player's to give.</returns>
    public static Task<int> LoadAsync(
        VortexDbContext dbCtx,
        int diskId,
        int playerId,
        int jukeboxId,
        CancellationToken ct
    ) =>
        dbCtx
            .Furnitures.Where(f =>
                f.Id == diskId
                && f.PlayerEntityId == playerId
                && f.RoomEntityId == null
                && f.WiredChestEntityId == null
                && f.JukeboxEntityId == null
                && f.DeletedAt == null
            )
            .ExecuteUpdateAsync(row => row.SetProperty(f => f.JukeboxEntityId, jukeboxId), ct);

    /// <summary>
    /// Takes a disk back out, to whoever owns it — which is not necessarily whoever is emptying the
    /// jukebox. A disk keeps its owner the whole time it is loaded, so clearing someone else's
    /// jukebox cannot be a way to collect their disks.
    /// </summary>
    /// <returns>1 when the disk moved, 0 when it was not in that jukebox.</returns>
    public static Task<int> UnloadAsync(
        VortexDbContext dbCtx,
        int diskId,
        int jukeboxId,
        CancellationToken ct
    ) =>
        dbCtx
            .Furnitures.Where(f =>
                f.Id == diskId && f.JukeboxEntityId == jukeboxId && f.DeletedAt == null
            )
            .ExecuteUpdateAsync(row => row.SetProperty(f => f.JukeboxEntityId, (int?)null), ct);

    /// <summary>
    /// The disks in one jukebox, in play order — which is insertion order, the only order there is.
    /// </summary>
    public static Task<List<JukeboxDiskRow>> ReadAsync(
        VortexDbContext dbCtx,
        int jukeboxId,
        int limit,
        CancellationToken ct
    ) =>
        dbCtx
            .Furnitures.AsNoTracking()
            .Where(f => f.JukeboxEntityId == jukeboxId && f.DeletedAt == null)
            .OrderBy(f => f.Id)
            .Take(limit)
            .Select(f => new JukeboxDiskRow(f.Id, f.PlayerEntityId, f.ExtraData))
            .ToListAsync(ct);
}

/// <summary>One loaded disk as the database has it.</summary>
internal readonly record struct JukeboxDiskRow(int DiskId, int OwnerId, string? ExtraData);
