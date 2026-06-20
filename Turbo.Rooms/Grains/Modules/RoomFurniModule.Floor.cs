using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Turbo.Logging;
using Turbo.Primitives;
using Turbo.Primitives.Action;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Object.Furniture;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Snapshots.Furniture;
using Turbo.Rooms.Object.Logic.Furniture.Floor;

namespace Turbo.Rooms.Grains.Modules;

public sealed partial class RoomFurniModule
{
    public async Task<bool> PlaceFloorItemAsync(
        ActionContext ctx,
        IRoomFloorItem item,
        int x,
        int y,
        Rotation rot,
        CancellationToken ct
    )
    {
        int tileIdx = _roomGrain.MapModule.ToIdx(x, y);

        if (!_roomGrain.MapModule.InBounds(tileIdx))
        {
            throw new TurboException(TurboErrorCodeEnum.TileOutOfBounds);
        }

        if (
            !await _roomGrain.ObjectModule.AttatchObjectAsync(item, ct)
            || !_roomGrain.MapModule.PlaceFloorItem(item, tileIdx, rot)
        )
        {
            return false;
        }

        await item.Logic.OnPlaceAsync(ctx, ct);

        item.MarkDirty();

        await _roomGrain.SendComposerToRoomAsync(item.GetAddComposer());

        return true;
    }

    public async Task<bool> MoveFloorItemByIdAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int x,
        int y,
        Altitude? z,
        Rotation? rot,
        CancellationToken ct
    )
    {
        if (
            !_roomGrain._state.ItemsById.TryGetValue(itemId, out IRoomItem? item)
            || item is not IRoomFloorItem floor
        )
        {
            throw new TurboException(TurboErrorCodeEnum.FloorItemNotFound);
        }

        int prevIdx = _roomGrain.MapModule.ToIdx(item.X, item.Y);
        int nextIdx = _roomGrain.MapModule.ToIdx(x, y);

        if (!_roomGrain.MapModule.MoveFloorItem(floor, nextIdx, z, rot))
        {
            return false;
        }

        await _roomGrain.SendComposerToRoomAsync(item.GetUpdateComposer());

        await item.Logic.OnMoveAsync(ctx, prevIdx, ct);

        return true;
    }

    public Task<bool> ValidateFloorItemPlacementAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int x,
        int y,
        Rotation rot
    )
    {
        if (
            !_roomGrain._state.ItemsById.TryGetValue(itemId, out IRoomItem? item)
            || item is not IRoomFloorItem tItem
        )
        {
            throw new TurboException(TurboErrorCodeEnum.FloorItemNotFound);
        }

        if (
            _roomGrain.MapModule.GetTileIdForSize(
                x,
                y,
                rot,
                tItem.Definition.Width,
                tItem.Definition.Length,
                out List<int> tileIds
            )
        )
        {
            foreach (int idx in tileIds)
            {
                RoomTileFlags tileFlags = _roomGrain._state.TileFlags[idx];
                Altitude tileHeight = _roomGrain._state.TileHeights[idx];
                RoomObjectId highestItemId = _roomGrain._state.TileHighestFloorItems[idx];
                bool isRotating = false;

                if (_roomGrain._state.ItemsById.TryGetValue(highestItemId, out IRoomItem? bItem))
                {
                    if (bItem == tItem)
                    {
                        tileHeight -= tItem.GetStackHeight();

                        if (tItem.Rotation != rot)
                        {
                            isRotating = true;
                        }
                    }
                }

                if (
                    tileFlags.Has(RoomTileFlags.Disabled)
                    || (tileHeight + tItem.GetStackHeight()) > _roomGrain._roomConfig.MaxStackHeight
                    || tileFlags.Has(RoomTileFlags.StackBlocked) && bItem != tItem
                    || (
                        !_roomGrain._roomConfig.PlaceItemsOnAvatars
                        && tileFlags.Has(RoomTileFlags.AvatarOccupied)
                        && !isRotating
                    )
                    || (
                        tileFlags.Has(RoomTileFlags.AvatarOccupied)
                        && (
                            !tileFlags.Has(RoomTileFlags.Walkable)
                            && !tileFlags.Has(RoomTileFlags.Sittable)
                            && !tileFlags.Has(RoomTileFlags.Layable)
                        )
                    )
                )
                {
                    return Task.FromResult(false);
                }

                if (bItem == tItem)
                {
                    continue;
                }

                if (bItem is not null && bItem != tItem)
                {
                    if (
                        bItem.Logic is FurnitureRollerLogic
                        && (
                            tItem.Definition.Width > 1
                            || tItem.Definition.Length > 1
                            || tItem.Logic is FurnitureRollerLogic
                        )
                    )
                    {
                        return Task.FromResult(false);
                    }

                    // if is a stack helper, allow placement
                }
            }
        }

        return Task.FromResult(true);
    }

    public Task<bool> ValidateNewFloorItemPlacementAsync(
        ActionContext ctx,
        IRoomFloorItem tItem,
        int x,
        int y,
        Rotation rot
    )
    {
        if (
            _roomGrain.MapModule.GetTileIdForSize(
                x,
                y,
                rot,
                tItem.Definition.Width,
                tItem.Definition.Length,
                out List<int> tileIds
            )
        )
        {
            foreach (int id in tileIds)
            {
                RoomTileFlags tileFlags = _roomGrain._state.TileFlags[id];
                Altitude tileHeight = _roomGrain._state.TileHeights[id];
                RoomObjectId highestItemId = _roomGrain._state.TileHighestFloorItems[id];
                IRoomFloorItem? bItem = null;

                if (
                    _roomGrain._state.ItemsById.TryGetValue(highestItemId, out IRoomItem? item)
                    && item is IRoomFloorItem floorItem
                )
                {
                    bItem = floorItem;
                }

                if (
                    tileFlags.Has(RoomTileFlags.Disabled)
                    || (tileHeight + tItem.GetStackHeight()) > _roomGrain._roomConfig.MaxStackHeight
                    || tileFlags.Has(RoomTileFlags.StackBlocked)
                    || (
                        !_roomGrain._roomConfig.PlaceItemsOnAvatars
                        && tileFlags.Has(RoomTileFlags.AvatarOccupied)
                    )
                    || (tileFlags.Has(RoomTileFlags.AvatarOccupied) && !tItem.Logic.CanWalk())
                )
                {
                    return Task.FromResult(false);
                }

                if (bItem is not null)
                {
                    if (
                        bItem.Logic is FurnitureRollerLogic
                        && (
                            tItem.Definition.Width > 1
                            || tItem.Definition.Length > 1
                            || tItem.Logic is FurnitureRollerLogic
                        )
                    )
                    {
                        return Task.FromResult(false);
                    }
                    // if is a stack helper, allow placement
                }
            }
        }

        return Task.FromResult(true);
    }

    public Task<ImmutableArray<RoomFloorItemSnapshot>> GetAllFloorItemSnapshotsAsync(
        CancellationToken ct
    ) =>
        Task.FromResult(
            _roomGrain
                ._state.ItemsById.Values.OfType<IRoomFloorItem>()
                .Select(x => x.GetSnapshot())
                .ToImmutableArray()
        );

    public bool GetTileIdForFloorItem(IRoomFloorItem item, out List<int> tileIds) =>
        _roomGrain.MapModule.GetTileIdForSize(
            item.X,
            item.Y,
            item.Rotation,
            item.Definition.Width,
            item.Definition.Length,
            out tileIds
        );

    /// <summary>
    /// Validates that an existing item (<paramref name="itemId"/>) can be moved to
    /// (<paramref name="x"/>, <paramref name="y"/>) within the footprint of the rented space
    /// <paramref name="spaceItem"/>. All target tiles must remain inside the space, and the
    /// normal tile-flag / height / stacking rules still apply.
    /// </summary>
    public async Task<bool> ValidateFloorItemMoveInRentedSpaceAsync(
        ActionContext ctx,
        RoomObjectId itemId,
        int x,
        int y,
        Rotation rot,
        IRoomFloorItem spaceItem
    )
    {
        if (
            !_roomGrain._state.ItemsById.TryGetValue(itemId, out IRoomItem? raw)
            || raw is not IRoomFloorItem tItem
        )
        {
            return false;
        }

        if (!GetTileIdForFloorItem(spaceItem, out List<int> spaceTileIds))
        {
            return false;
        }

        if (
            !_roomGrain.MapModule.GetTileIdForSize(
                x,
                y,
                rot,
                tItem.Definition.Width,
                tItem.Definition.Length,
                out List<int> newItemTileIds
            )
        )
        {
            return false;
        }

        HashSet<int> spaceTileSet = [.. spaceTileIds];

        if (!newItemTileIds.All(spaceTileSet.Contains))
        {
            return false;
        }

        return await ValidateFloorItemPlacementAsync(ctx, itemId, x, y, rot);
    }

    /// <summary>
    /// Validates that <paramref name="tItem"/> can be placed at (<paramref name="x"/>,
    /// <paramref name="y"/>) within the footprint of the rented space
    /// <paramref name="spaceItem"/>. All target tiles must fall inside the space's tiles,
    /// and the normal tile-flag / height / stacking rules still apply.
    /// </summary>
    public async Task<bool> ValidateNewFloorItemPlacementInRentedSpaceAsync(
        ActionContext ctx,
        IRoomFloorItem tItem,
        int x,
        int y,
        Rotation rot,
        IRoomFloorItem spaceItem
    )
    {
        if (!GetTileIdForFloorItem(spaceItem, out List<int> spaceTileIds))
        {
            return false;
        }

        if (
            !_roomGrain.MapModule.GetTileIdForSize(
                x,
                y,
                rot,
                tItem.Definition.Width,
                tItem.Definition.Length,
                out List<int> newItemTileIds
            )
        )
        {
            return false;
        }

        HashSet<int> spaceTileSet = [.. spaceTileIds];

        if (!newItemTileIds.All(spaceTileSet.Contains))
        {
            return false;
        }

        return await ValidateNewFloorItemPlacementAsync(ctx, tItem, x, y, rot);
    }
}
