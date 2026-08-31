using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Vortex.Logging.Extensions;
using Vortex.Primitives;
using Vortex.Primitives.Action;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Events.Bots;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Snapshots.Avatars;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// Everything that moves a bot. A bot walks the same way a pet does — plan a path, take one tile per
/// tick — because it is the same problem and the room already solves it well.
/// <para>
/// Three things can move it, in order of standing: a wired order, following somebody, and its own
/// wandering. They are exclusive per tick, so a bot under orders does not also stroll.
/// </para>
/// </summary>
public sealed partial class RoomBotSystem
{
    /// <summary>How far a bot will pick a destination from where it stands.</summary>
    private const int WanderRadius = 5;

    private const int WanderAttempts = 6;

    private const int WanderIdleMinMs = 4_000;
    private const int WanderIdleMaxMs = 15_000;

    private readonly Dictionary<int, List<int>> _pathByBotId = [];
    private readonly Dictionary<int, long> _nextWanderAtMsByBotId = [];

    /// <summary>
    /// Where a wired stack sent a bot. An order outranks wandering — a builder who told a bot to go
    /// somewhere should not watch it stroll off instead — and is forgotten once the bot arrives.
    /// </summary>
    private readonly Dictionary<int, int> _orderedGoalTileByBotId = [];

    /// <summary>Who a bot is following, until a stack tells it to stop.</summary>
    private readonly Dictionary<int, PlayerId> _followTargetByBotId = [];

    /// <summary>
    /// Advances every bot that has somewhere to be by at most one tile and returns the ones that
    /// moved, so the caller can broadcast a single update rather than one packet per bot.
    /// </summary>
    private List<RoomAvatarSnapshot> StepBots(long now)
    {
        List<RoomAvatarSnapshot> moved = [];

        foreach (BotSnapshot bot in _botsById.Values.OrderBy(b => b.BotId).ToArray())
        {
            BotSnapshot? stepped =
                StepFollowingBot(bot) ?? StepOrderedBot(bot) ?? StepWanderingBot(bot, now);

            if (stepped is null)
            {
                continue;
            }

            moved.Add(ToAvatarSnapshot(stepped));
            PublishArrival(stepped);
        }

        return moved;
    }

    /// <summary>
    /// Tells the room where a bot has just arrived, which is what the two bot triggers listen for.
    /// Published per step rather than per tick, so a bot standing still fires nothing.
    /// </summary>
    private void PublishArrival(BotSnapshot bot)
    {
        int tileIdx = _roomGrain.MapModule.ToIdx(bot.X, bot.Y);

        _roomGrain
            .PublishRoomEventAsync(
                new BotReachedTileEvent
                {
                    RoomId = _roomGrain.RoomId,
                    BotId = bot.BotId,
                    BotName = bot.Name,
                    ObjectId = ToRoomObjectId(bot.BotId),
                    TileIdx = tileIdx,
                },
                CancellationToken.None
            )
            .LogAndForget(
                _roomGrain._logger,
                "Failed to publish bot arrival in room {RoomId}",
                _roomGrain._state.RoomId
            );

        foreach (PlayerId reached in PlayersBeside(bot))
        {
            _roomGrain
                .PublishRoomEventAsync(
                    new BotReachedAvatarEvent
                    {
                        RoomId = _roomGrain.RoomId,
                        // The person reached is the cause, so a stack can go on to act on them as
                        // its triggered user.
                        CausedBy = ActionContext.CreateForPlayer(reached, _roomGrain.RoomId),
                        BotId = bot.BotId,
                        BotName = bot.Name,
                        ObjectId = ToRoomObjectId(bot.BotId),
                        ReachedPlayerId = reached,
                    },
                    CancellationToken.None
                )
                .LogAndForget(
                    _roomGrain._logger,
                    "Failed to publish bot meeting in room {RoomId}",
                    _roomGrain._state.RoomId
                );
        }
    }

    /// <summary>Whoever the bot is now standing next to, itself included in neither sense.</summary>
    private List<PlayerId> PlayersBeside(BotSnapshot bot)
    {
        List<PlayerId> beside = [];

        foreach ((PlayerId playerId, RoomObjectId objectId) in _roomGrain._state.AvatarsByPlayerId)
        {
            if (
                _roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out IRoomAvatar? avatar)
                && IsAdjacentTo(bot, avatar.X, avatar.Y)
            )
            {
                beside.Add(playerId);
            }
        }

        return beside;
    }

    /// <summary>
    /// A bot walking towards the person it follows. It re-plans whenever its path runs out, which is
    /// what keeps it with somebody who is walking away, and stops on a neighbouring tile rather than
    /// trying to stand where the player already is.
    /// </summary>
    private BotSnapshot? StepFollowingBot(BotSnapshot bot)
    {
        if (!_followTargetByBotId.TryGetValue(bot.BotId, out PlayerId target))
        {
            return null;
        }

        if (
            !_roomGrain._state.AvatarsByPlayerId.TryGetValue(target, out RoomObjectId objectId)
            || !_roomGrain._state.AvatarsByObjectId.TryGetValue(objectId, out IRoomAvatar? avatar)
        )
        {
            // They left. The order stands — a bot told to follow somebody who steps out and back in
            // picks them up again — but there is nothing to walk towards meanwhile.
            return null;
        }

        if (IsAdjacentTo(bot, avatar.X, avatar.Y))
        {
            _ = _pathByBotId.Remove(bot.BotId);

            return null;
        }

        if (!_pathByBotId.TryGetValue(bot.BotId, out List<int>? path) || path.Count == 0)
        {
            if (!TryPlanPathTo(bot, avatar.X, avatar.Y, out path))
            {
                return null;
            }

            _pathByBotId[bot.BotId] = path;
        }

        return StepAlong(bot, path);
    }

    /// <summary>A bot walking out a wired order, which it forgets on arrival.</summary>
    private BotSnapshot? StepOrderedBot(BotSnapshot bot)
    {
        if (!_orderedGoalTileByBotId.TryGetValue(bot.BotId, out int goalTileId))
        {
            return null;
        }

        if (!_pathByBotId.TryGetValue(bot.BotId, out List<int>? path) || path.Count == 0)
        {
            (int goalX, int goalY) = _roomGrain.MapModule.GetTileXY(goalTileId);

            if (!TryPlanPathTo(bot, goalX, goalY, out path))
            {
                // Nowhere to walk: blocked, or the bot is already standing there. Either way the
                // order is spent, and keeping it would have the bot retrying it every tick forever.
                _ = _orderedGoalTileByBotId.Remove(bot.BotId);

                return null;
            }

            _pathByBotId[bot.BotId] = path;
        }

        BotSnapshot? stepped = StepAlong(bot, path);

        if (path.Count == 0)
        {
            _ = _orderedGoalTileByBotId.Remove(bot.BotId);
        }

        return stepped;
    }

    private BotSnapshot? StepWanderingBot(BotSnapshot bot, long now)
    {
        if (!IsWanderEnabled(bot.BotId))
        {
            return null;
        }

        if (!_pathByBotId.TryGetValue(bot.BotId, out List<int>? path) || path.Count == 0)
        {
            // Idle: wait out the pause, then look for somewhere to go. Scheduling on first sight
            // rather than walking immediately keeps a freshly loaded room from having every bot set
            // off at once.
            if (!_nextWanderAtMsByBotId.TryGetValue(bot.BotId, out long dueAt))
            {
                _nextWanderAtMsByBotId[bot.BotId] = ScheduleNextWanderAt(now);

                return null;
            }

            if (now < dueAt || !TryPlanWander(bot, out path))
            {
                return null;
            }

            _pathByBotId[bot.BotId] = path;
        }

        BotSnapshot? stepped = StepAlong(bot, path);

        if (stepped is null || path.Count == 0)
        {
            _nextWanderAtMsByBotId[bot.BotId] = ScheduleNextWanderAt(now);
        }

        return stepped;
    }

    /// <summary>
    /// Takes the next tile of a path. A step that fails drops the rest of it rather than walking
    /// through whatever took the tile in the meantime.
    /// </summary>
    private BotSnapshot? StepAlong(BotSnapshot bot, List<int> path)
    {
        if (path.Count == 0)
        {
            return null;
        }

        int nextTileId = path[0];
        path.RemoveAt(0);

        if (path.Count == 0)
        {
            _ = _pathByBotId.Remove(bot.BotId);
        }

        BotSnapshot? stepped = TryStepTo(bot, nextTileId);

        if (stepped is null)
        {
            _ = _pathByBotId.Remove(bot.BotId);
        }

        return stepped;
    }

    private BotSnapshot? TryStepTo(BotSnapshot bot, int nextTileId)
    {
        if (!_roomGrain.MapModule.InBounds(nextTileId))
        {
            return null;
        }

        (int nextX, int nextY) = _roomGrain.MapModule.GetTileXY(nextTileId);

        if (!IsTileFreeForBot(nextX, nextY))
        {
            return null;
        }

        BotSnapshot updated = bot with
        {
            X = nextX,
            Y = nextY,
            Z = _roomGrain._state.TileHeights[nextTileId],
            Rotation = RotationExtensions.FromPoints(bot.X, bot.Y, nextX, nextY),
        };

        _botsById[bot.BotId] = updated;

        return updated;
    }

    private bool TryPlanWander(BotSnapshot bot, out List<int> path)
    {
        path = [];

        for (int attempt = 0; attempt < WanderAttempts; attempt++)
        {
            int targetX = bot.X + Random.Shared.Next(-WanderRadius, WanderRadius + 1);
            int targetY = bot.Y + Random.Shared.Next(-WanderRadius, WanderRadius + 1);

            if ((targetX == bot.X && targetY == bot.Y) || !IsTileFreeForBot(targetX, targetY))
            {
                continue;
            }

            if (TryPlanPathTo(bot, targetX, targetY, out path))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// A walkable route from where the bot stands to a tile, minus the tile it is already on.
    /// </summary>
    private bool TryPlanPathTo(BotSnapshot bot, int targetX, int targetY, out List<int> path)
    {
        path = [];

        if (
            !_roomGrain.MapModule.InBounds(bot.X, bot.Y)
            || !_roomGrain.MapModule.InBounds(targetX, targetY)
        )
        {
            return false;
        }

        // A bot walks flat: it has its own free-tile rule and never had a height test, so every
        // step is allowed and lands where the tile's top surface is. Giving bots the avatars' 3D
        // search is a separate question -- they would need somewhere to remember which surface they
        // are on, which a bot currently has no field for.
        IReadOnlyList<(int X, int Y)> found = _roomGrain.PathingSystem.FindPath(
            (bot.X, bot.Y),
            (targetX, targetY),
            tileId =>
            {
                (int x, int y) = _roomGrain.MapModule.GetTileXY(tileId);

                return IsTileFreeForBot(x, y);
            },
            (_, _, _) => true
        );

        // A path of one is the bot's own tile, which is not a walk.
        if (found.Count < 2)
        {
            return false;
        }

        path = [.. found.Skip(1).Select(pos => _roomGrain.MapModule.ToIdx(pos.X, pos.Y))];

        return true;
    }

    /// <summary>
    /// Close enough to have arrived. A follower that insisted on the exact tile would never stop,
    /// because the person it follows is standing on it.
    /// </summary>
    private static bool IsAdjacentTo(BotSnapshot bot, int x, int y) =>
        Math.Abs(bot.X - x) <= 1 && Math.Abs(bot.Y - y) <= 1;

    /// <summary>
    /// Spread rather than fixed, for the same reason chatter is: bots stepping on one interval move
    /// as a block, which looks like choreography rather than life.
    /// </summary>
    private static long ScheduleNextWanderAt(long now) =>
        now + Random.Shared.Next(WanderIdleMinMs, WanderIdleMaxMs + 1);

    /// <summary>
    /// The walk button is a toggle and the client sends empty data on every click, so the state is
    /// the server's to keep.
    /// </summary>
    private bool IsWanderEnabled(int botId) =>
        _skillsByBotId.TryGetValue(botId, out Dictionary<string, string>? skills)
        && IsFlagOn(
            skills.GetValueOrDefault(
                BotSkillId.RandomWalk.ToString(CultureInfo.InvariantCulture),
                string.Empty
            )
        );
}
