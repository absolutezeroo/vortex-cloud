using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Action;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Furniture.Snapshots.StuffData;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Players;
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

    public async Task ProcessUserMovementAsync(
        IRoomAvatar avatar,
        int tileIdx,
        SlideAvatarMoveType moveType,
        WiredWalkMode walkMode = WiredWalkMode.KeepIfCloser
    )
    {
        if (avatar is null || !_roomGrain.MapModule.InBounds(tileIdx))
        {
            return;
        }

        try
        {
            (int sourceX, int sourceY, Altitude sourceZ) = (avatar.X, avatar.Y, avatar.Z);
            Altitude targetZ = _roomGrain._state.TileHeights[tileIdx];

            // Capture the walk before clearing it: "keep walking" means resuming towards the same goal
            // from wherever the user lands, not replaying a path that was routed from the tile they are
            // being moved off.
            bool wasWalking = avatar.IsWalking;
            int goalTileId = avatar.GoalTileId;
            int sourceTileIdx = _roomGrain.MapModule.ToIdx(sourceX, sourceY);

            // The queued path is stale the moment the avatar is relocated — the walk tick steps through
            // TilePath, so leaving it in place walks the user straight back towards where they started.
            _roomGrain.AvatarModule.CancelWalk(avatar);

            if (_roomGrain.MapModule.RollAvatar(avatar, tileIdx, targetZ))
            {
                // Face the direction moved (unless it was an in-place move).
                Rotation facing =
                    sourceX != avatar.X || sourceY != avatar.Y
                        ? RotationExtensions.FromPoints(sourceX, sourceY, avatar.X, avatar.Y)
                        : avatar.Rotation;

                avatar.SetBodyRotation(facing);
                avatar.SetHeadRotation(facing);

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
                        BodyDirection = facing,
                        HeadDirection = facing,
                    }
                );

                await ResumeWalkAfterMoveAsync(
                    avatar,
                    walkMode,
                    wasWalking,
                    goalTileId,
                    sourceTileIdx,
                    tileIdx
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
    }

    /// <summary>
    /// Applies the effect's walk mode after the user has been relocated: re-routes them towards the goal
    /// they were heading for (from where they landed), or leaves them stopped. Only ever resumes a walk
    /// that was actually in progress and still has a reachable goal.
    /// </summary>
    private async Task ResumeWalkAfterMoveAsync(
        IRoomAvatar avatar,
        WiredWalkMode walkMode,
        bool wasWalking,
        int goalTileId,
        int sourceTileIdx,
        int targetTileIdx
    )
    {
        if (
            walkMode == WiredWalkMode.Stop
            || !wasWalking
            || goalTileId < 0
            || goalTileId == targetTileIdx
        )
        {
            return;
        }

        if (
            walkMode == WiredWalkMode.KeepIfCloser
            && StepsBetween(targetTileIdx, goalTileId) >= StepsBetween(sourceTileIdx, goalTileId)
        )
        {
            return;
        }

        (int goalX, int goalY) = _roomGrain.MapModule.GetTileXY(goalTileId);

        await _roomGrain.AvatarModule.WalkAvatarToAsync(
            avatar,
            goalX,
            goalY,
            System.Threading.CancellationToken.None
        );
    }

    /// <summary>Steps between two tiles. Avatars move in eight directions, so a diagonal costs the same
    /// as a straight step and the step count is the Chebyshev distance.</summary>
    private int StepsBetween(int fromTileIdx, int toTileIdx)
    {
        (int fromX, int fromY) = _roomGrain.MapModule.GetTileXY(fromTileIdx);
        (int toX, int toY) = _roomGrain.MapModule.GetTileXY(toTileIdx);

        return Math.Max(Math.Abs(toX - fromX), Math.Abs(toY - fromY));
    }

    public Task ProcessUserDirectionAsync(
        IRoomAvatar avatar,
        Rotation bodyRotation,
        Rotation headRotation
    )
    {
        if (avatar is null)
        {
            return Task.CompletedTask;
        }

        avatar.SetBodyRotation(bodyRotation);
        avatar.SetHeadRotation(headRotation);

        UserDirections.Add(
            new()
            {
                ObjectId = avatar.ObjectId,
                BodyRotation = avatar.Rotation,
                HeadRotation = avatar.HeadRotation,
            }
        );

        return Task.CompletedTask;
    }

    public ActionContext AsActionContext() => ActionContext.CreateForWired(_roomGrain.RoomId);

    public Task SendComposerToRoomAsync(IComposer composer) =>
        _roomGrain.SendComposerToRoomAsync(composer);

    /// <summary>
    /// Makes the named bot speak. Named rather than addressed by id because that is how the client's
    /// setup form asks for it — the builder types a name into the box — and an unknown name is a
    /// stack pointing at a bot that has been picked up, which is ordinary rather than an error.
    /// </summary>
    public Task<bool> ProcessBotChatAsync(
        string botName,
        string text,
        WiredBotChatType chatType,
        PlayerId? whisperTo
    ) =>
        WithBotAsync(
            botName,
            "speak",
            bot =>
                _roomGrain.BotSystem.SayAsync(
                    bot.BotId,
                    text,
                    chatType,
                    whisperTo,
                    CancellationToken.None
                )
        );

    public Task<bool> ProcessBotMovementAsync(string botName, int tileIdx, bool instant)
    {
        if (!_roomGrain.MapModule.InBounds(tileIdx))
        {
            return Task.FromResult(false);
        }

        (int x, int y) = _roomGrain.MapModule.GetTileXY(tileIdx);

        return WithBotAsync(
            botName,
            "move",
            bot =>
                instant
                    ? _roomGrain.BotSystem.TeleportAsync(bot.BotId, x, y, CancellationToken.None)
                    : _roomGrain.BotSystem.WalkToAsync(bot.BotId, x, y, CancellationToken.None)
        );
    }

    public Task<bool> ProcessBotFollowAsync(string botName, PlayerId? target) =>
        WithBotAsync(
            botName,
            "follow",
            bot =>
                _roomGrain.BotSystem.SetFollowTargetAsync(bot.BotId, target, CancellationToken.None)
        );

    public Task<bool> ProcessBotFigureAsync(string botName, string figure) =>
        WithBotAsync(
            botName,
            "change look",
            bot => _roomGrain.BotSystem.SetFigureAsync(bot.BotId, figure, CancellationToken.None)
        );

    /// <summary>
    /// Runs something against the bot going by a name. A stack naming a bot that is not here is
    /// ordinary rather than exceptional — somebody picked it up — so it answers false and says
    /// nothing.
    /// </summary>
    private async Task<bool> WithBotAsync(string botName, string what, Func<BotSnapshot, Task> act)
    {
        try
        {
            BotSnapshot? bot = await _roomGrain
                .BotSystem.FindBotByNameAsync(botName, CancellationToken.None)
                .ConfigureAwait(true);

            if (bot is null)
            {
                return false;
            }

            await act(bot).ConfigureAwait(true);

            return true;
        }
        catch (Exception ex)
        {
            _roomGrain._logger.LogWarning(
                ex,
                "Failed to make bot {BotName} {What} in room {RoomId}.",
                botName,
                what,
                _roomGrain.RoomId
            );

            return false;
        }
    }
}
