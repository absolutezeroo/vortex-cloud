using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Snapshots.Avatars;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// Wandering. A bot walks the same way a pet does — pick somewhere nearby, path to it, take one
/// tile per tick — because it is the same problem and the room already solves it well.
/// </summary>
public sealed partial class RoomBotSystem
{
    /// <summary>The wander skill's id, alongside the chatter skill at 2.</summary>
    private const int WanderCommandId = 0;

    /// <summary>How far a bot will pick a destination from where it stands.</summary>
    private const int WanderRadius = 5;

    private const int WanderAttempts = 6;

    private const int WanderIdleMinMs = 4_000;
    private const int WanderIdleMaxMs = 15_000;

    private readonly Dictionary<int, List<int>> _pathByBotId = [];
    private readonly Dictionary<int, long> _nextWanderAtMsByBotId = [];

    /// <summary>
    /// Advances every wandering bot by at most one tile and returns the ones that moved, so the
    /// caller can broadcast a single update rather than one packet per bot.
    /// </summary>
    private List<RoomAvatarSnapshot> StepWanderingBots(long now)
    {
        List<RoomAvatarSnapshot> moved = [];

        foreach (BotSnapshot bot in _botsById.Values.OrderBy(b => b.BotId).ToArray())
        {
            if (!IsWanderEnabled(bot.BotId))
            {
                continue;
            }

            if (!_pathByBotId.TryGetValue(bot.BotId, out List<int>? path) || path.Count == 0)
            {
                // Idle: wait out the pause, then look for somewhere to go. Scheduling on first
                // sight rather than walking immediately keeps a freshly loaded room from having
                // every bot set off at once.
                if (!_nextWanderAtMsByBotId.TryGetValue(bot.BotId, out long dueAt))
                {
                    _nextWanderAtMsByBotId[bot.BotId] = ScheduleNextWanderAt(now);
                    continue;
                }

                if (now < dueAt || !TryPlanWander(bot, out path))
                {
                    continue;
                }

                _pathByBotId[bot.BotId] = path;
            }

            int nextTileId = path[0];
            path.RemoveAt(0);

            if (path.Count == 0)
            {
                _ = _pathByBotId.Remove(bot.BotId);
                _nextWanderAtMsByBotId[bot.BotId] = ScheduleNextWanderAt(now);
            }

            BotSnapshot? stepped = TryStepTo(bot, nextTileId);

            if (stepped is null)
            {
                // Something took the tile between planning and arriving. Drop the rest of the path
                // rather than walking through it, and pick a new destination next time round.
                _ = _pathByBotId.Remove(bot.BotId);
                _nextWanderAtMsByBotId[bot.BotId] = ScheduleNextWanderAt(now);

                continue;
            }

            moved.Add(ToAvatarSnapshot(stepped));
        }

        return moved;
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

        if (!_roomGrain.MapModule.InBounds(bot.X, bot.Y))
        {
            return false;
        }

        for (int attempt = 0; attempt < WanderAttempts; attempt++)
        {
            int targetX = bot.X + Random.Shared.Next(-WanderRadius, WanderRadius + 1);
            int targetY = bot.Y + Random.Shared.Next(-WanderRadius, WanderRadius + 1);

            if (
                (targetX == bot.X && targetY == bot.Y)
                || !_roomGrain.MapModule.InBounds(targetX, targetY)
                || !IsTileFreeForBot(targetX, targetY)
            )
            {
                continue;
            }

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
                continue;
            }

            path = [.. found.Skip(1).Select(pos => _roomGrain.MapModule.ToIdx(pos.X, pos.Y))];

            return true;
        }

        return false;
    }

    /// <summary>
    /// Spread rather than fixed, for the same reason chatter is: bots stepping on one interval move
    /// as a block, which looks like choreography rather than life.
    /// </summary>
    private static long ScheduleNextWanderAt(long now) =>
        now + Random.Shared.Next(WanderIdleMinMs, WanderIdleMaxMs + 1);

    /// <summary>Any non-empty configuration turns wandering on; the value itself is the client's.</summary>
    private bool IsWanderEnabled(int botId) =>
        _skillsByBotId.TryGetValue(botId, out Dictionary<string, string>? skills)
        && skills.TryGetValue(
            WanderCommandId.ToString(CultureInfo.InvariantCulture),
            out string? value
        )
        && !string.IsNullOrWhiteSpace(value);
}
