using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Turbo.Primitives.Action;
using Turbo.Primitives.Furniture.Snapshots.StuffData;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Enums.Wired;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Object.Furniture;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Furniture.Wall;
using Turbo.Primitives.Rooms.Snapshots.Wired;
using Turbo.Primitives.Rooms.Wired;
using Turbo.Rooms.Grains;

namespace Turbo.Rooms.Wired;

public sealed class WiredExecutionContext(RoomGrain roomGrain)
    : WiredContext(roomGrain),
        IWiredExecutionContext
{
    public List<WiredUserMovementSnapshot> UserMoves { get; } = [];
    public List<WiredFloorItemMovementSnapshot> FloorItemMoves { get; } = [];
    public List<WiredWallItemMovementSnapshot> WallItemMoves { get; } = [];
    public List<WiredUserDirectionSnapshot> UserDirections { get; } = [];
    public List<(RoomObjectId, StuffDataSnapshot)> FloorItemStateUpdates { get; } = [];
    public List<(RoomObjectId, string)> WallItemStateUpdates { get; } = [];

    public async Task ProcessItemStateUpdateAsync(IRoomItem item, int state)
    {
        if (item is null)
        {
            return;
        }

        try
        {
            await item.Logic.SetStateAsync(state, false);

            switch (item)
            {
                case IRoomFloorItem:
                    FloorItemStateUpdates.Add((item.ObjectId, item.Logic.StuffData.GetSnapshot()));
                    break;
                case IRoomWallItem:
                    WallItemStateUpdates.Add((item.ObjectId, item.Logic.GetLegacyString()));
                    break;
            }
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to process wired state update ({State}) for item {ItemId}.",
                state,
                item.ObjectId
            );
        }
    }

    public Task ProcessFloorItemMovementAsync(
        IRoomFloorItem floorItem,
        int tileIdx,
        Altitude? z,
        Rotation? rot
    )
    {
        if (floorItem is null)
        {
            return Task.CompletedTask;
        }

        try
        {
            (int sourceX, int sourceY, Altitude sourceZ) = (floorItem.X, floorItem.Y, floorItem.Z);

            Altitude finalZ = z ?? floorItem.Z;
            Rotation finalRot = rot ?? floorItem.Rotation;

            if (_roomGrain.MapModule.MoveFloorItem(floorItem, tileIdx, z, rot))
            {
                FloorItemMoves.Add(
                    new()
                    {
                        ObjectId = floorItem.ObjectId,
                        SourceX = sourceX,
                        SourceY = sourceY,
                        SourceZ = sourceZ,
                        TargetX = floorItem.X,
                        TargetY = floorItem.Y,
                        TargetZ = floorItem.Z,
                        Rotation = floorItem.Rotation,
                        AnimationTime =
                            Policy.AnimationMode == WiredAnimationModeType.Instant
                                ? 0
                                : Policy.AnimationTimeMs,
                    }
                );
            }
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to process wired floor-item movement for item {ItemId} to tile {TileIdx}.",
                floorItem.ObjectId,
                tileIdx
            );
        }

        return Task.CompletedTask;
    }

    public Task ProcessWallItemMovementAsync(
        IRoomWallItem wallItem,
        int x,
        int y,
        Altitude z,
        Rotation rot,
        int wallOffset
    )
    {
        if (wallItem is null)
        {
            return Task.CompletedTask;
        }

        try
        {
            (int sourceX, int sourceY, Altitude sourceZ, Rotation sourceRot, int sourceOffset) = (
                wallItem.X,
                wallItem.Y,
                wallItem.Z,
                wallItem.Rotation,
                wallItem.WallOffset
            );

            if (_roomGrain.MapModule.MoveWallItem(wallItem, x, y, z, rot, wallOffset))
            {
                WallItemMoves.Add(
                    new()
                    {
                        ObjectId = wallItem.ObjectId,
                        IsDirectionRight = wallItem.Rotation != Rotation.South,
                        SourceX = sourceX,
                        SourceY = sourceY,
                        SourceWallOffset = sourceOffset,
                        SourceZ = (int)sourceZ,
                        TargetX = wallItem.X,
                        TargetY = wallItem.Y,
                        TargetWallOffset = wallItem.WallOffset,
                        TargetZ = (int)wallItem.Z,
                    }
                );
            }
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to process wired wall-item movement for item {ItemId} to ({X}, {Y}).",
                wallItem.ObjectId,
                x,
                y
            );
        }

        return Task.CompletedTask;
    }

    public ActionContext AsActionContext() => ActionContext.CreateForWired(_roomGrain.RoomId);

    public Task SendComposerToRoomAsync(IComposer composer) =>
        Room.SendComposerToRoomAsync(composer);
}
