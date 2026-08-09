using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Messages.Outgoing.Room.Engine;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Snapshots.Avatars;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// What something other than the bot's owner can tell it to do — today, a wired stack. Kept apart
/// from the menu skills because these are orders rather than settings: none of them is remembered
/// past the room's lifetime except the look, which is the bot's own.
/// </summary>
public sealed partial class RoomBotSystem
{
    /// <summary>
    /// Puts a bot on a tile at once, with no walk. Returns false when the tile will not take it,
    /// which is the honest answer for a teleport pad buried under furniture.
    /// </summary>
    public async Task<bool> TeleportAsync(int botId, int x, int y, CancellationToken ct)
    {
        await EnsureBotsLoadedAsync(ct).ConfigureAwait(true);

        if (!_botsById.TryGetValue(botId, out BotSnapshot? bot) || !IsTileFreeForBot(x, y))
        {
            return false;
        }

        // Whatever it was doing, it is not doing it here. Leaving the path in place would have the
        // bot walk straight back towards where it was standing.
        _ = _pathByBotId.Remove(botId);
        _ = _orderedGoalTileByBotId.Remove(botId);

        BotSnapshot moved = bot with
        {
            X = x,
            Y = y,
            Z = _roomGrain._state.TileHeights[_roomGrain.MapModule.ToIdx(x, y)],
        };

        _botsById[botId] = moved;

        await BroadcastMovedAsync(moved, ct).ConfigureAwait(true);

        return true;
    }

    /// <summary>
    /// Sends a bot walking to a tile. The walk itself happens on the room tick, one tile at a time,
    /// so this only records where it is headed.
    /// </summary>
    public async Task<bool> WalkToAsync(int botId, int x, int y, CancellationToken ct)
    {
        await EnsureBotsLoadedAsync(ct).ConfigureAwait(true);

        if (!_botsById.ContainsKey(botId) || !_roomGrain.MapModule.InBounds(x, y))
        {
            return false;
        }

        _orderedGoalTileByBotId[botId] = _roomGrain.MapModule.ToIdx(x, y);

        // The old path led somewhere else; the next tick plans a new one from where the bot stands.
        _ = _pathByBotId.Remove(botId);

        return true;
    }

    /// <summary>
    /// Starts or stops a bot following somebody. A null target stops it, which is what the wired
    /// form's second radio button asks for.
    /// </summary>
    public async Task<bool> SetFollowTargetAsync(int botId, PlayerId? target, CancellationToken ct)
    {
        await EnsureBotsLoadedAsync(ct).ConfigureAwait(true);

        if (!_botsById.ContainsKey(botId))
        {
            return false;
        }

        if (target is { } player)
        {
            _followTargetByBotId[botId] = player;
        }
        else
        {
            _ = _followTargetByBotId.Remove(botId);
            _ = _pathByBotId.Remove(botId);
        }

        return true;
    }

    /// <summary>
    /// Dresses a bot in a given look. Unlike the menu's dress-up this takes a figure string rather
    /// than reading one off an avatar, because the wired form captures the look when it is saved
    /// rather than when it runs.
    /// </summary>
    public async Task<bool> SetFigureAsync(int botId, string figure, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(figure))
        {
            return false;
        }

        await EnsureBotsLoadedAsync(ct).ConfigureAwait(true);

        if (!_botsById.TryGetValue(botId, out BotSnapshot? bot))
        {
            return false;
        }

        await using VortexDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        BotEntity? entity = await dbCtx
            .Bots.SingleOrDefaultAsync(
                b => b.Id == botId && b.RoomEntityId == _roomGrain.RoomId.Value,
                ct
            )
            .ConfigureAwait(true);

        if (entity is null)
        {
            return false;
        }

        entity.Figure = figure.Trim();

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);

        BotSnapshot changed = bot with { Figure = entity.Figure };
        _botsById[botId] = changed;

        BroadcastLook(changed);

        return true;
    }

    /// <summary>One bot's redraw, sent the same way the tick sends a roomful of them.</summary>
    private async Task BroadcastMovedAsync(BotSnapshot bot, CancellationToken ct) =>
        await _roomGrain
            .SendComposerToRoomAsync(
                new UserUpdateMessageComposer
                {
                    Avatars =
                    [
                        ToAvatarSnapshot(
                            bot,
                            await GetOwnerNameAsync(bot.OwnerId, ct).ConfigureAwait(true)
                        ),
                    ],
                }
            )
            .ConfigureAwait(true);
}
