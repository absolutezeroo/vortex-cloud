using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Logging;
using Vortex.Logging.Extensions;
using Vortex.Primitives;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Snapshots.Avatars;
using Vortex.Protocol.Messages.Outgoing.Room.Action;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;

namespace Vortex.Rooms.Grains.Systems;

public sealed class RoomAvatarTickSystem(RoomGrain roomGrain)
{
    private readonly RoomGrain _roomGrain = roomGrain;

    // Reused across ticks — RoomAvatarTickSystem is one instance per room grain, which is
    // single-threaded, so clearing and refilling this per tick is safe and avoids a per-tick
    // allocation proportional to room count × tick rate.
    private readonly List<RoomAvatarSnapshot> _dirtySnapshots = [];

    public async Task ProcessAvatarsAsync(long now, CancellationToken ct)
    {
        if (now < _roomGrain._state.NextAvatarBoundaryMs)
        {
            return;
        }

        _roomGrain._state.NextAvatarBoundaryMs = _roomGrain.AdvanceBoundaryPast(
            now,
            _roomGrain._roomConfig.AvatarTickMs
        );

        _dirtySnapshots.Clear();

        foreach (IRoomAvatar avatar in _roomGrain._state.AvatarsByObjectId.Values)
        {
            try
            {
                await _roomGrain.AvatarModule.ProcessNextAvatarStepAsync(avatar, ct);

                if (avatar.TilePath.Count <= 0)
                {
                    if (avatar.PendingStopAtMs > 0 && now < avatar.PendingStopAtMs)
                    {
                        continue;
                    }

                    await _roomGrain.AvatarModule.StopWalkingAsync(avatar, ct);

                    if (avatar.NeedsInvoke)
                    {
                        await _roomGrain.MapModule.InvokeAvatarAsync(avatar, ct);
                    }
                }
                else
                {
                    await ProcessAvatarAsync(avatar, now, ct);
                }

                if (avatar.IsDirty)
                {
                    _dirtySnapshots.Add(avatar.GetSnapshot());
                }

                // A sign is a one-shot: it has been on the wire by now (either in the update above or
                // in the immediate broadcast the sign handler sent), so drop it. Without this the
                // avatar holds the sign up forever.
                if (avatar.HasStatus(AvatarStatusType.Sign))
                {
                    avatar.RemoveStatus(AvatarStatusType.Sign);
                }

                ExpireHandItem(avatar, now);
            }
            catch (Exception ex)
            {
                _roomGrain._logger.LogWarning(
                    ex,
                    "Failed to process avatar tick for avatar {ObjectId} in room {RoomId}.",
                    avatar.ObjectId,
                    _roomGrain.RoomId
                );
            }
        }

        if (_dirtySnapshots.Count == 0)
        {
            return;
        }

        _roomGrain
            .SendComposerToRoomAsync(
                new UserUpdateMessageComposer { Avatars = [.. _dirtySnapshots] }
            )
            .LogAndForget(
                _roomGrain._logger,
                "Failed to publish avatar tick update for room {RoomId}",
                _roomGrain.RoomId
            );
    }

    /// <summary>
    /// Takes a held item out of the hand once its time is up. Habbo hand items are shown for a
    /// while and then gone; without this one drink would be held for the rest of the session.
    /// </summary>
    private void ExpireHandItem(IRoomAvatar avatar, long now)
    {
        if (avatar.CarryItemId == 0 || now < avatar.CarryItemUntilMs)
        {
            return;
        }

        avatar.SetCarryItem(0, 0);

        // The item is not in the avatar block, so an emptied hand has to be said out loud or the
        // client keeps drawing what it was last told about.
        _roomGrain
            .SendComposerToRoomAsync(
                new CarryObjectMessageComposer { UserId = avatar.ObjectId.Value, ItemType = 0 }
            )
            .LogAndForget(
                _roomGrain._logger,
                "Failed to clear a hand item in room {RoomId}",
                _roomGrain.RoomId
            );
    }

    private async Task ProcessAvatarAsync(IRoomAvatar avatar, long now, CancellationToken ct)
    {
        int nextTileId = avatar.TilePath[0];
        avatar.TilePath.RemoveAt(0);

        if (avatar.TilePath.Count == 0)
        {
            avatar.PendingStopAtMs = _roomGrain.AlignToNextBoundary(
                now,
                _roomGrain._roomConfig.AvatarTickMs
            );
        }

        await ValidateAvatarStepAsync(avatar, nextTileId, now, ct);
    }

    private async Task ValidateAvatarStepAsync(
        IRoomAvatar avatar,
        int nextTileId,
        long now,
        CancellationToken ct
    )
    {
        try
        {
            bool isGoal = avatar.TilePath.Count == 0;
            int prevTileId = _roomGrain.MapModule.ToIdx(avatar.X, avatar.Y);
            (int nextX, int nextY) = _roomGrain.MapModule.GetTileXY(nextTileId);
            Altitude prevHeight = _roomGrain.MapModule.GetTileHeightForAvatar(prevTileId);
            Altitude nextHeight = _roomGrain.MapModule.GetTileHeightForAvatar(nextTileId);

            if (Math.Abs(nextHeight - prevHeight) > Math.Abs(_roomGrain._roomConfig.MaxStepHeight))
            {
                throw new VortexException(VortexErrorCodeEnum.InvalidMoveTarget);
            }

            if (!_roomGrain.MapModule.CanAvatarWalkBetween(avatar, prevTileId, nextTileId, isGoal))
            {
                if (!isGoal)
                {
                    (int goalX, int goalY) = _roomGrain.MapModule.GetTileXY(avatar.GoalTileId);

                    if (await _roomGrain.AvatarModule.WalkAvatarToAsync(avatar, goalX, goalY, ct))
                    {
                        await ProcessAvatarAsync(avatar, now, ct);

                        return;
                    }
                }

                throw new VortexException(VortexErrorCodeEnum.InvalidMoveTarget);
            }

            RoomObjectId prevHighestItemId = _roomGrain._state.TileHighestFloorItems[prevTileId];
            RoomObjectId nextHighestItemId = _roomGrain._state.TileHighestFloorItems[nextTileId];

            if (
                prevHighestItemId > 0
                && _roomGrain._state.ItemsById.TryGetValue(
                    prevHighestItemId,
                    out IRoomItem? prevFloorItem
                )
            )
            {
                await ((IRoomFloorItem)prevFloorItem).Logic.OnWalkOffAsync(
                    (IRoomAvatarContext)avatar.Logic.Context,
                    ct
                );
            }

            _roomGrain.MapModule.RemoveAvatarAtIdx(avatar, prevTileId, false);
            _roomGrain.MapModule.AddAvatarAtIdx(avatar, nextTileId, false);

            if (
                nextHighestItemId > 0
                && _roomGrain._state.ItemsById.TryGetValue(
                    nextHighestItemId,
                    out IRoomItem? nextFloorItem
                )
            )
            {
                await ((IRoomFloorItem)nextFloorItem).Logic.OnWalkOnAsync(
                    (IRoomAvatarContext)avatar.Logic.Context,
                    ct
                );
            }

            avatar.RemoveStatus(AvatarStatusType.Lay, AvatarStatusType.Sit);
            avatar.AddStatus(AvatarStatusType.Move, $"{nextX},{nextY},{nextHeight}");
            avatar.SetRotation(RotationExtensions.FromPoints(avatar.X, avatar.Y, nextX, nextY));

            avatar.NextTileId = nextTileId;
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to advance avatar {ObjectId} to next tile in room {RoomId}; stopping walk.",
                avatar.ObjectId,
                _roomGrain.RoomId
            );

            await _roomGrain.AvatarModule.StopWalkingAsync(avatar, ct);
        }
    }
}
