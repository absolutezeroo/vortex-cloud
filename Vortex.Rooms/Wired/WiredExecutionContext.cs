using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Snapshots.StuffData;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Furniture.Wall;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired;

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
                        SourceOffsetX = sourceOffset,
                        SourceOffsetY = (int)sourceZ,
                        TargetX = wallItem.X,
                        TargetY = wallItem.Y,
                        TargetOffsetX = wallItem.WallOffset,
                        TargetOffsetY = (int)wallItem.Z,
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
                "Failed to process wired wall-item movement for item {ItemId} to ({X}, {Y}).",
                wallItem.ObjectId,
                x,
                y
            );
        }

        return Task.CompletedTask;
    }

    public Task ProcessUserMovementAsync(
        IRoomAvatar avatar,
        int tileIdx,
        SlideAvatarMoveType moveType
    )
    {
        if (avatar is null || !_roomGrain.MapModule.InBounds(tileIdx))
        {
            return Task.CompletedTask;
        }

        try
        {
            (int sourceX, int sourceY, Altitude sourceZ) = (avatar.X, avatar.Y, avatar.Z);
            Altitude targetZ = _roomGrain._state.TileHeights[tileIdx];

            if (_roomGrain.MapModule.RollAvatar(avatar, tileIdx, targetZ))
            {
                UserMoves.Add(
                    new()
                    {
                        ObjectId = avatar.ObjectId,
                        SourceX = sourceX,
                        SourceY = sourceY,
                        SourceZ = sourceZ,
                        TargetX = avatar.X,
                        TargetY = avatar.Y,
                        TargetZ = avatar.Z,
                        MoveType = moveType,
                        AnimationTime =
                            moveType == SlideAvatarMoveType.Slide ? Policy.AnimationTimeMs : 0,
                        BodyDirection = avatar.Rotation,
                        HeadDirection = avatar.HeadRotation,
                    }
                );
            }
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to process wired user movement for avatar {ObjectId} to tile {TileIdx}.",
                avatar.ObjectId,
                tileIdx
            );
        }

        return Task.CompletedTask;
    }

    public ActionContext AsActionContext() => ActionContext.CreateForWired(_roomGrain.RoomId);

    public Task SendComposerToRoomAsync(IComposer composer) =>
        Room.SendComposerToRoomAsync(composer);
}
