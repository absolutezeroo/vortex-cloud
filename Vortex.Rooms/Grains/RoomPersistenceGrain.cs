using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Furniture;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Snapshots.Furniture;
using Vortex.Rooms.Configuration;

namespace Vortex.Rooms.Grains;

public sealed class RoomPersistenceGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IOptions<RoomConfig> roomConfig,
    ILogger<IRoomPersistenceGrain> logger
) : Grain, IRoomPersistenceGrain
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;

    private readonly Dictionary<long, RoomItemSnapshot> _dirtyItems = [];
    private readonly ILogger<IRoomPersistenceGrain> _logger = logger;
    private readonly HashSet<RoomObjectId> _removedItemIds = [];
    private readonly RoomConfig _roomConfig = roomConfig.Value;
    private IDisposable? _timer;

    public Task EnqueueDirtyItemAsync(
        RoomId roomId,
        RoomItemSnapshot snapshot,
        CancellationToken ct,
        bool remove = false
    )
    {
        _dirtyItems[snapshot.ObjectId] = snapshot;

        if (remove)
        {
            _removedItemIds.Add(snapshot.ObjectId);
        }

        return Task.CompletedTask;
    }

    public Task EnqueueDirtyItemsAsync(
        RoomId roomId,
        List<RoomItemSnapshot> snapshots,
        CancellationToken ct
    )
    {
        foreach (RoomItemSnapshot snapshot in snapshots)
        {
            _dirtyItems[snapshot.ObjectId] = snapshot;
        }

        return Task.CompletedTask;
    }

    public override Task OnActivateAsync(CancellationToken ct)
    {
        _timer = this.RegisterGrainTimer<object?>(
            static async (self, ct) => await ((RoomPersistenceGrain)self!).FlushDirtyItemsAsync(ct),
            this,
            TimeSpan.FromMilliseconds(_roomConfig.DirtyItemsTickMs),
            TimeSpan.FromMilliseconds(_roomConfig.DirtyItemsTickMs)
        );

        return Task.CompletedTask;
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        // Drain rather than flush once. A flush writes at most MaxDirtyItemsPerFlush, which is the
        // right bound for a timer -- it stops one busy room from holding a database connection for a
        // whole build session -- and the wrong one here: a room deactivating with 300 moved items
        // wrote 100 of them and dropped the rest on the floor.
        //
        // Bounded by progress, not by a count: the loop stops the moment a pass fails to shrink the
        // queue, so a database that is refusing writes costs one extra attempt instead of spinning
        // through deactivation.
        while (_dirtyItems.Count > 0)
        {
            int before = _dirtyItems.Count;

            await FlushDirtyItemsAsync(ct);

            if (_dirtyItems.Count >= before)
            {
                break;
            }
        }
    }

    private async Task FlushDirtyItemsAsync(CancellationToken ct)
    {
        if (_dirtyItems.Count == 0)
        {
            return;
        }

        RoomItemSnapshot[] batch = _dirtyItems
            .Take(_roomConfig.MaxDirtyItemsPerFlush)
            .Select(x => x.Value)
            .ToArray();

        // Removed after the save, not before it. Taken off the queue up front, a batch that then
        // failed to save was gone: the catch below logged it and the positions were lost until
        // somebody moved the furniture again. One connection blip cost a room its layout, quietly.
        //
        // The grain is not reentrant, so nothing can enqueue during the await -- the queue this
        // returns to is the one it left.
        try
        {
            using VortexDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct);

            int roomId = (int)this.GetPrimaryKeyLong();

            // A removal says "this item left room A", and it is written up to DirtyItemsTickMs after
            // the item did. Pick a sofa up in A and drop it in B inside that window and B claims the
            // row first, then A's flush arrives and writes RoomEntityId = null over it: the sofa
            // vanishes from B and reappears in the inventory, with two grains writing one row and no
            // order between them (ROOM-PER-005).
            //
            // So a removal is conditional on the row still being ours. Loading the candidates rather
            // than attaching them blind is the condition: the rows another room has already taken do
            // not come back, so nothing is written for them.
            //
            // ponytail: read-then-write, not a conditional claim. It closes a two-second window down
            // to one query, which is the whole of the reported bug; the last of it needs the
            // per-item claim primitive (ECON-ITM-004).
            HashSet<int> removing =
            [
                .. batch
                    .Where(item => _removedItemIds.Contains(item.ObjectId))
                    .Select(item => item.ObjectId.Value),
            ];

            Dictionary<int, FurnitureEntity> stillOurs =
                removing.Count == 0
                    ? []
                    : await dbCtx
                        .Furnitures.Where(f => removing.Contains(f.Id) && f.RoomEntityId == roomId)
                        .ToDictionaryAsync(f => f.Id, ct);

            foreach (RoomItemSnapshot item in batch)
            {
                if (removing.Contains(item.ObjectId.Value))
                {
                    // Gone from this room already, by another room's hand. Nothing to write, and the
                    // queue drops it below like everything else in the batch.
                    if (!stillOurs.TryGetValue(item.ObjectId.Value, out FurnitureEntity? leaving))
                    {
                        continue;
                    }

                    leaving.PlayerEntityId = item.OwnerId.Value;
                    leaving.X = item.X;
                    leaving.Y = item.Y;
                    leaving.Z = item.Z;
                    leaving.Rotation = item.Rotation;
                    leaving.ExtraData = item.ExtraData;
                    leaving.RoomEntityId = null;

                    if (item is RoomWallItemSnapshot leavingWallItem)
                    {
                        leaving.WallOffset = leavingWallItem.WallOffset;
                    }

                    continue;
                }

                FurnitureEntity dbEntity = new()
                {
                    Id = item.ObjectId.Value,
                    PlayerEntityId = item.OwnerId.Value,
                    X = item.X,
                    Y = item.Y,
                    Z = item.Z,
                    Rotation = item.Rotation,
                    ExtraData = item.ExtraData,
                };

                dbCtx.Attach(dbEntity);

                EntityEntry<FurnitureEntity> e = dbCtx.Entry(dbEntity);

                e.Property(x => x.PlayerEntityId).IsModified = true;
                e.Property(x => x.RoomEntityId).IsModified = true;
                e.Property(x => x.X).IsModified = true;
                e.Property(x => x.Y).IsModified = true;
                e.Property(x => x.Z).IsModified = true;
                e.Property(x => x.Rotation).IsModified = true;
                e.Property(x => x.ExtraData).IsModified = true;

                if (item is RoomWallItemSnapshot wallItem)
                {
                    dbEntity.WallOffset = wallItem.WallOffset;

                    e.Property(x => x.WallOffset).IsModified = true;
                }

                dbEntity.RoomEntityId = roomId;
            }

            await dbCtx.SaveChangesAsync(ct);

            foreach (RoomItemSnapshot item in batch)
            {
                _dirtyItems.Remove(item.ObjectId);
                _removedItemIds.Remove(item.ObjectId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to flush {Count} dirty furniture items for room {RoomId}",
                batch.Length,
                this.GetPrimaryKeyLong()
            );
        }
    }
}
