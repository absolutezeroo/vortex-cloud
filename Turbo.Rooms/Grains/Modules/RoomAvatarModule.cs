using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Turbo.Logging;
using Turbo.Logging.Extensions;
using Turbo.Primitives;
using Turbo.Primitives.Action;
using Turbo.Primitives.Messages.Outgoing.Room.Action;
using Turbo.Primitives.Messages.Outgoing.Room.Chat;
using Turbo.Primitives.Messages.Outgoing.Room.Engine;
using Turbo.Primitives.Orleans.Snapshots.Players;
using Turbo.Primitives.Players;
using Turbo.Primitives.Rooms.Enums;
using Turbo.Primitives.Rooms.Object;
using Turbo.Primitives.Rooms.Object.Avatars;
using Turbo.Primitives.Rooms.Snapshots.Avatars;

namespace Turbo.Rooms.Grains.Modules;

public sealed partial class RoomAvatarModule(RoomGrain roomGrain)
{
    private readonly RoomGrain _roomGrain = roomGrain;

    private int _nextObjectId = 0;

    public async Task<IRoomAvatar> CreateAvatarFromPlayerAsync(
        ActionContext ctx,
        PlayerSummarySnapshot snapshot,
        CancellationToken ct
    )
    {
        int objectId = _nextObjectId += 1;
        int startX = _roomGrain._state.Model?.DoorX ?? 0;
        int startY = _roomGrain._state.Model?.DoorY ?? 0;
        Rotation startRot = _roomGrain._state.Model?.DoorRotation ?? Rotation.North;

        if (!_roomGrain.MapModule.InBounds(startX, startY))
        {
            // TODO get a valid tile
            startX = 0;
            startY = 0;
            startRot = Rotation.North;
        }

        IRoomPlayer avatar = _roomGrain._avatarProvider.CreateAvatarFromPlayerSnapshot(
            objectId,
            snapshot
        );

        RoomControllerType controllerLevel =
            await _roomGrain.SecurityModule.GetControllerLevelAsync(ctx);

        avatar.AddStatus(AvatarStatusType.FlatControl, ((int)controllerLevel).ToString());

        avatar.NextTileId = _roomGrain.MapModule.ToIdx(startX, startY);

        await _roomGrain.ObjectModule.AttatchObjectAsync(avatar, ct);

        _roomGrain._state.AvatarsByPlayerId[snapshot.PlayerId] = avatar.ObjectId;

        avatar.SetRotation(startRot);

        return avatar;
    }

    public async Task RemoveAvatarFromPlayerAsync(
        ActionContext ctx,
        PlayerId playerId,
        CancellationToken ct
    )
    {
        try
        {
            if (
                !_roomGrain._state.AvatarsByPlayerId.TryGetValue(
                    playerId,
                    out RoomObjectId objectId
                )
                || !_roomGrain._state.AvatarsByObjectId.TryGetValue(
                    objectId,
                    out IRoomAvatar? avatar
                )
            )
            {
                return;
            }

            await _roomGrain.ObjectModule.RemoveObjectAsync(ctx, avatar, ct, -1);

            _roomGrain._state.AvatarsByPlayerId.Remove(playerId);
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to remove avatar for player {PlayerId} in room {RoomId}.",
                playerId,
                _roomGrain.RoomId
            );
        }
    }

    public async Task<bool> WalkAvatarToAsync(
        ActionContext ctx,
        int targetX,
        int targetY,
        CancellationToken ct
    )
    {
        if (
            ctx.PlayerId <= 0
            || !_roomGrain._state.AvatarsByPlayerId.TryGetValue(
                ctx.PlayerId,
                out RoomObjectId objectIdValue
            )
            || !_roomGrain._state.AvatarsByObjectId.TryGetValue(
                objectIdValue,
                out IRoomAvatar? avatar
            )
            || !await WalkAvatarToAsync(avatar, targetX, targetY, ct)
        )
        {
            return false;
        }

        return true;
    }

    public async Task<bool> WalkAvatarToAsync(
        RoomObjectId objectId,
        int targetX,
        int targetY,
        CancellationToken ct
    )
    {
        if (
            !_roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out IRoomAvatar? avatar)
            || !await WalkAvatarToAsync(avatar, targetX, targetY, ct)
        )
        {
            return false;
        }

        return true;
    }

    public async Task<bool> WalkAvatarToAsync(
        IRoomAvatar avatar,
        int targetX,
        int targetY,
        CancellationToken ct
    )
    {
        try
        {
            int goalTileId = _roomGrain.MapModule.ToIdx(targetX, targetY);
            int currentTileId =
                avatar.NextTileId > 0
                    ? avatar.NextTileId
                    : _roomGrain.MapModule.ToIdx(avatar.X, avatar.Y);
            (int currentX, int currentY) = _roomGrain.MapModule.GetTileXY(currentTileId);

            if ((goalTileId == currentTileId) || !avatar.SetGoalTileId(goalTileId))
            {
                return false;

                //throw new TurboException(TurboErrorCodeEnum.InvalidMoveTarget);
            }

            IReadOnlyList<(int X, int Y)> path = _roomGrain.PathingSystem.FindPath(
                avatar,
                (currentX, currentY),
                (targetX, targetY)
            );

            if (path.Count == 0)
            {
                return false;

                // throw new TurboException(TurboErrorCodeEnum.InvalidMoveTarget);
            }

            avatar.TilePath.Clear();
            avatar.TilePath.AddRange(
                path.Skip(1).Select(pos => _roomGrain.MapModule.ToIdx(pos.X, pos.Y))
            );

            avatar.IsWalking = true;

            return true;
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to walk avatar {ObjectId} to ({TargetX},{TargetY}) in room {RoomId}.",
                avatar.ObjectId,
                targetX,
                targetY,
                _roomGrain.RoomId
            );

            await StopWalkingAsync(avatar, ct);

            return false;
        }
    }

    public Task<ImmutableArray<RoomAvatarSnapshot>> GetAllAvatarSnapshotsAsync(
        CancellationToken ct
    ) =>
        Task.FromResult(
            _roomGrain
                ._state.AvatarsByObjectId.Values.Select(x => x.GetSnapshot())
                .ToImmutableArray()
        );

    public async Task StopWalkingAsync(IRoomAvatar avatar, CancellationToken ct)
    {
        try
        {
            if (!avatar.IsWalking)
            {
                return;
            }

            avatar.IsWalking = false;
            avatar.NextMoveStepAtMs = 0;
            avatar.NextMoveUpdateAtMs = 0;
            avatar.PendingStopAtMs = 0;

            await ProcessNextAvatarStepAsync(avatar, ct);

            avatar.TilePath.Clear();
            avatar.NextTileId = -1;
            avatar.SetGoalTileId(-1);
            avatar.RemoveStatus(AvatarStatusType.Move);
            avatar.NeedsInvoke = true;
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to stop walking for avatar {ObjectId} in room {RoomId}.",
                avatar.ObjectId,
                _roomGrain.RoomId
            );
        }
    }

    public async Task ProcessNextAvatarStepAsync(IRoomAvatar avatar, CancellationToken ct)
    {
        try
        {
            int nextTileId = avatar.NextTileId;

            if (nextTileId < 0)
            {
                return;
            }

            avatar.NextTileId = -1;

            int prevTileId = _roomGrain.MapModule.ToIdx(avatar.X, avatar.Y);
            (int nextX, int nextY) = _roomGrain.MapModule.GetTileXY(nextTileId);

            if (prevTileId == nextTileId)
            {
                return;
            }

            _roomGrain.MapModule.RemoveAvatar(avatar, false);

            avatar.SetPosition(nextX, nextY);

            _roomGrain.MapModule.AddAvatar(avatar, false);
            _roomGrain.MapModule.UpdateHeightForAvatar(avatar);
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to process next avatar step for avatar {ObjectId} in room {RoomId}.",
                avatar.ObjectId,
                _roomGrain.RoomId
            );

            await StopWalkingAsync(avatar, ct);
        }
    }

    public Task<bool> UpdateAvatarWithPlayerAsync(
        PlayerSummarySnapshot snapshot,
        CancellationToken ct
    )
    {
        if (
            snapshot.PlayerId <= 0
            || !_roomGrain._state.AvatarsByPlayerId.TryGetValue(
                snapshot.PlayerId,
                out RoomObjectId objectId
            )
            || !_roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out IRoomAvatar? avatar)
            || avatar is not IRoomPlayer avatarPlayer
            || !avatarPlayer.UpdateWithPlayer(snapshot)
        )
        {
            return Task.FromResult(false);
        }

        _roomGrain
            .SendComposerToRoomAsync(
                new UserChangeMessageComposer
                {
                    ObjectId = avatarPlayer.ObjectId,
                    Figure = avatarPlayer.Figure,
                    Gender = avatarPlayer.Gender,
                    CustomInfo = avatarPlayer.Motto,
                    AchievementScore = snapshot.AchievementScore,
                }
            )
            .LogAndForget(
                _roomGrain._logger,
                "Failed to publish avatar update for room {RoomId}",
                _roomGrain._state.RoomId
            );

        return Task.FromResult(true);
    }

    public Task<bool> SetAvatarDanceAsync(
        RoomObjectId objectId,
        AvatarDanceType danceType,
        CancellationToken ct
    )
    {
        if (
            objectId <= 0
            || !_roomGrain._state.AvatarsByObjectId.TryGetValue(
                objectId.Value,
                out IRoomAvatar? avatar
            )
            || avatar is not IRoomPlayer player
            || !player.SetDance(danceType)
        )
        {
            return Task.FromResult(false);
        }

        _roomGrain
            .SendComposerToRoomAsync(
                new DanceMessageComposer
                {
                    ObjectId = avatar.ObjectId,
                    DanceType = player.DanceType,
                }
            )
            .LogAndForget(
                _roomGrain._logger,
                "Failed to publish avatar update for room {RoomId}",
                _roomGrain._state.RoomId
            );

        return Task.FromResult(true);
    }

    public Task<bool> SetAvatarExpressionAsync(
        RoomObjectId objectId,
        AvatarExpressionType expressionType,
        CancellationToken ct
    )
    {
        if (
            objectId <= 0
            || !_roomGrain._state.AvatarsByObjectId.TryGetValue(
                objectId.Value,
                out IRoomAvatar? avatar
            )
        )
        {
            return Task.FromResult(false);
        }

        _roomGrain
            .SendComposerToRoomAsync(
                new ExpressionMessageComposer
                {
                    ObjectId = avatar.ObjectId,
                    ExpressionType = expressionType,
                }
            )
            .LogAndForget(
                _roomGrain._logger,
                "Failed to publish avatar update for room {RoomId}",
                _roomGrain._state.RoomId
            );

        return Task.FromResult(true);
    }

    public Task<bool> SetAvatarPostureAsync(RoomObjectId objectId, CancellationToken ct)
    {
        if (
            objectId <= 0
            || !_roomGrain._state.AvatarsByObjectId.TryGetValue(
                objectId.Value,
                out IRoomAvatar? avatar
            )
        )
        {
            return Task.FromResult(false);
        }

        bool isSitting = avatar.HasStatus(AvatarStatusType.Sit);
        avatar.Sit(!isSitting);

        _roomGrain
            .SendComposerToRoomAsync(
                new UserUpdateMessageComposer { Avatars = [avatar.GetSnapshot()] }
            )
            .LogAndForget(
                _roomGrain._logger,
                "Failed to publish avatar update for room {RoomId}",
                _roomGrain._state.RoomId
            );

        return Task.FromResult(true);
    }

    public Task<bool> SetAvatarSignAsync(RoomObjectId objectId, int signId, CancellationToken ct)
    {
        if (
            objectId <= 0
            || !_roomGrain._state.AvatarsByObjectId.TryGetValue(
                objectId.Value,
                out IRoomAvatar? avatar
            )
        )
        {
            return Task.FromResult(false);
        }

        avatar.AddStatus(AvatarStatusType.Sign, signId.ToString());

        _roomGrain
            .SendComposerToRoomAsync(
                new UserUpdateMessageComposer { Avatars = [avatar.GetSnapshot()] }
            )
            .LogAndForget(
                _roomGrain._logger,
                "Failed to publish avatar update for room {RoomId}",
                _roomGrain._state.RoomId
            );

        return Task.FromResult(true);
    }

    public Task<bool> LookToAvatarAsync(
        RoomObjectId objectId,
        int targetX,
        int targetY,
        CancellationToken ct
    )
    {
        if (
            objectId <= 0
            || !_roomGrain._state.AvatarsByObjectId.TryGetValue(
                objectId.Value,
                out IRoomAvatar? avatar
            )
        )
        {
            return Task.FromResult(false);
        }

        int dx = targetX - avatar.X;
        int dy = targetY - avatar.Y;

        if (dx == 0 && dy == 0)
        {
            return Task.FromResult(false);
        }

        Rotation rotation = RotationExtensions.FromDelta(dx, dy);
        avatar.SetHeadRotation(rotation);

        if (!avatar.HasStatus(AvatarStatusType.Sit, AvatarStatusType.Lay))
        {
            avatar.SetBodyRotation(rotation);
        }

        _roomGrain
            .SendComposerToRoomAsync(
                new UserUpdateMessageComposer { Avatars = [avatar.GetSnapshot()] }
            )
            .LogAndForget(
                _roomGrain._logger,
                "Failed to publish avatar update for room {RoomId}",
                _roomGrain._state.RoomId
            );

        return Task.FromResult(true);
    }

    public Task SetAvatarTypingAsync(RoomObjectId objectId, bool isTyping, CancellationToken ct)
    {
        if (
            objectId <= 0
            || !_roomGrain._state.AvatarsByObjectId.TryGetValue(
                objectId.Value,
                out IRoomAvatar? avatar
            )
        )
        {
            return Task.CompletedTask;
        }

        _roomGrain
            .SendComposerToRoomAsync(
                new UserTypingMessageComposer
                {
                    UserId = avatar.ObjectId.Value,
                    IsTyping = isTyping,
                }
            )
            .LogAndForget(
                _roomGrain._logger,
                "Failed to publish avatar update for room {RoomId}",
                _roomGrain._state.RoomId
            );

        return Task.CompletedTask;
    }
}
