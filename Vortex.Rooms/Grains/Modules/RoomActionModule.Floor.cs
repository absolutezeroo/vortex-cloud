using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Logging;
using Vortex.Primitives;
using Vortex.Primitives.Action;
using Vortex.Primitives.Events;
using Vortex.Primitives.Inventory.Grains;
using Vortex.Primitives.Inventory.Snapshots;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Protocol.Messages.Incoming.Userdefinedroomevents;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired;

namespace Vortex.Rooms.Grains.Modules;

public sealed partial class RoomActionModule
{
    public async Task<bool> PlaceFloorItemAsync(
        ActionContext ctx,
        FurnitureItemSnapshot snapshot,
        int x,
        int y,
        Rotation rot,
        CancellationToken ct
    )
    {
        IRoomItem item = _roomGrain._itemsLoader.CreateFromFurnitureItemSnapshot(snapshot);

        if (item is not IRoomFloorItem floorItem)
        {
            throw new VortexException(VortexErrorCodeEnum.FloorItemNotFound);
        }

        IRoomFloorItem? rentedSpace = null;

        if (await _roomGrain.SecurityModule.CanManipulateFurniAsync(ctx))
        {
            if (
                !await _roomGrain.FurniModule.ValidateNewFloorItemPlacementAsync(
                    ctx,
                    floorItem,
                    x,
                    y,
                    rot
                )
            )
            {
                throw new VortexException(VortexErrorCodeEnum.InvalidMoveTarget);
            }
        }
        else
        {
            rentedSpace = await _roomGrain.SecurityModule.FindRentedSpaceForPlayerAsync(
                ctx.PlayerId.Value,
                ct
            );

            if (rentedSpace is null)
            {
                throw new VortexException(VortexErrorCodeEnum.NoPermissionToPlaceFurni);
            }

            if (
                !await _roomGrain.FurniModule.ValidateNewFloorItemPlacementInRentedSpaceAsync(
                    ctx,
                    floorItem,
                    x,
                    y,
                    rot,
                    rentedSpace
                )
            )
            {
                throw new VortexException(VortexErrorCodeEnum.InvalidMoveTarget);
            }
        }

        if (!await _roomGrain.FurniModule.PlaceFloorItemAsync(ctx, floorItem, x, y, rot, ct))
        {
            return false;
        }

        IInventoryGrain inventory = _roomGrain._grainFactory.GetInventoryGrain(item.OwnerId);

        await inventory.RemoveFurnitureAsync(item.ObjectId, ct);

        if (rentedSpace is not null)
        {
            try
            {
                await using VortexDbContext db =
                    await _roomGrain._dbCtxFactory.CreateDbContextAsync(ct);
                await db
                    .Furnitures.Where(f => f.Id == floorItem.ObjectId.Value && f.DeletedAt == null)
                    .ExecuteUpdateAsync(
                        s =>
                            s.SetProperty(
                                f => f.RentableSpaceFurnitureEntityId,
                                (int?)rentedSpace.ObjectId.Value
                            ),
                        ct
                    );
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogWarning(
                    ex,
                    "Failed to tag furniture {FurniId} with rentable space {SpaceId}",
                    floorItem.ObjectId.Value,
                    rentedSpace.ObjectId.Value
                );
            }
        }

        // Detached: the room is not waiting for the placer's quest, achievement and daily-task
        // grains to answer before the next click can be served. See RoomGrain.PublishDetached.
        _roomGrain.PublishDetached(
            new ItemPlacedEvent(
                item.ObjectId.Value,
                ctx.PlayerId.Value,
                item.OwnerId.Value,
                _roomGrain.RoomId.Value,
                JsonSerializer.Serialize(
                    new
                    {
                        x,
                        y,
                        rotation = rot.ToString(),
                    }
                ),
                snapshot.Definition.Id
            )
        );

        return true;
    }

    public async Task<bool> MoveFloorItemByIdAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int x,
        int y,
        Rotation rot,
        CancellationToken ct
    )
    {
        if (await _roomGrain.SecurityModule.CanManipulateFurniAsync(ctx))
        {
            if (
                !await _roomGrain.FurniModule.ValidateFloorItemPlacementAsync(
                    ctx,
                    itemId,
                    x,
                    y,
                    rot
                )
            )
            {
                throw new VortexException(VortexErrorCodeEnum.InvalidMoveTarget);
            }
        }
        else
        {
            IRoomFloorItem? rentedSpace =
                await _roomGrain.SecurityModule.FindRentedSpaceForOwnedItemAsync(ctx, itemId, ct);

            if (rentedSpace is null)
            {
                throw new VortexException(VortexErrorCodeEnum.NoPermissionToManipulateFurni);
            }

            if (
                !await _roomGrain.FurniModule.ValidateFloorItemMoveInRentedSpaceAsync(
                    ctx,
                    itemId,
                    x,
                    y,
                    rot,
                    rentedSpace
                )
            )
            {
                throw new VortexException(VortexErrorCodeEnum.InvalidMoveTarget);
            }
        }

        // Read before the move, because after it there is nothing left to compare against: the
        // client turns a piece by moving it to the square it is already on.
        bool rotatedInPlace =
            _roomGrain._state.ItemsById.TryGetValue(itemId, out IRoomItem? before)
            && before is IRoomFloorItem previous
            && previous.GetSnapshot() is { } was
            && was.X == x
            && was.Y == y
            && was.Rotation != rot;

        if (!await _roomGrain.FurniModule.MoveFloorItemByIdAsync(ctx, itemId, x, y, null, rot, ct))
        {
            return false;
        }

        // Detached, and this is the one that fires most: a drag across a room is a move per tile.
        _roomGrain.PublishDetached(
            new ItemMovedEvent(
                itemId.Value,
                ctx.PlayerId.Value,
                _roomGrain.RoomId.Value,
                JsonSerializer.Serialize(
                    new
                    {
                        x,
                        y,
                        rotation = rot.ToString(),
                    }
                ),
                rotatedInPlace
            )
        );

        return true;
    }

    public async Task<bool> ApplyWiredUpdateAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        WiredUpdateRequest update,
        CancellationToken ct
    )
    {
        if (!_roomGrain._state.ItemsById.TryGetValue(itemId, out IRoomItem? item))
        {
            throw new VortexException(VortexErrorCodeEnum.FloorItemNotFound);
        }

        if (item.Logic is not FurnitureWiredLogic wiredLogic)
        {
            throw new VortexException(VortexErrorCodeEnum.FloorItemNotFound);
        }

        if (!await wiredLogic.ApplyWiredUpdateAsync(ctx, update, ct))
        {
            return false;
        }

        return true;
    }
}
