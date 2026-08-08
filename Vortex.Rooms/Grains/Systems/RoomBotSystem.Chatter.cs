using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Bots;
using Vortex.Primitives.Messages.Outgoing.Room.Chat;

namespace Vortex.Rooms.Grains.Systems;

/// <summary>
/// What makes a placed bot more than scenery: it says the phrases its owner configured. Kept apart
/// from placement because this runs on the room tick and that does not.
/// </summary>
public sealed partial class RoomBotSystem
{
    /// <summary>The chatter skill's id, from the client's own BotChatterMarkovConfiguration.</summary>
    private const int ChatterCommandId = 2;

    /// <summary>How the client packs a phrase list into one string.</summary>
    private const char ChatterSeparator = ';';

    private readonly Dictionary<int, long> _nextChatterAtMsByBotId = [];

    /// <summary>Phrase lists as loaded, so a talking bot is not a database read per tick.</summary>
    private readonly Dictionary<int, string[]> _chatterByBotId = [];

    private bool _chatterLoaded;

    public async Task ProcessBotsAsync(long now, CancellationToken ct)
    {
        if (now < _roomGrain._state.NextBotBoundaryMs)
        {
            return;
        }

        // Snap forward rather than stepping. Catching up one tick at a time is fine for the
        // millisecond drift this normally sees, but a room that was paused — or a clock that jumped
        // — turns that into a loop of millions of iterations while holding the grain's only turn.
        _roomGrain._state.NextBotBoundaryMs =
            now - ((now - _roomGrain._state.NextBotBoundaryMs) % BotTickMs) + BotTickMs;

        await EnsureBotsLoadedAsync(ct).ConfigureAwait(true);

        if (_botsById.Count == 0)
        {
            return;
        }

        await EnsureChatterLoadedAsync(ct).ConfigureAwait(true);

        foreach (BotSnapshot bot in _botsById.Values.OrderBy(b => b.BotId).ToArray())
        {
            if (
                !_chatterByBotId.TryGetValue(bot.BotId, out string[]? phrases)
                || phrases.Length == 0
            )
            {
                continue;
            }

            // First sight of a bot schedules it rather than making it speak, so a room full of bots
            // does not greet every visitor in chorus the moment it activates.
            if (!_nextChatterAtMsByBotId.TryGetValue(bot.BotId, out long dueAt))
            {
                _nextChatterAtMsByBotId[bot.BotId] = ScheduleNextChatterAt(now);
                continue;
            }

            if (now < dueAt)
            {
                continue;
            }

            _nextChatterAtMsByBotId[bot.BotId] = ScheduleNextChatterAt(now);

            await _roomGrain
                .SendComposerToRoomAsync(
                    new ChatMessageComposer
                    {
                        ObjectId = ToRoomObjectId(bot.BotId),
                        Text = phrases[Random.Shared.Next(phrases.Length)],
                        Gesture = default,
                        StyleId = 0,
                        Links = [],
                        TrackingId = 0,
                    }
                )
                .ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Spread out rather than fixed: bots on the same interval would fall into lockstep and speak
    /// as one, which reads as a bug even though each is behaving.
    /// </summary>
    private static long ScheduleNextChatterAt(long now) =>
        now + Random.Shared.Next(ChatterMinIntervalMs, ChatterMaxIntervalMs + 1);

    private async Task EnsureChatterLoadedAsync(CancellationToken ct)
    {
        if (_chatterLoaded)
        {
            return;
        }

        await using VortexDbContext dbCtx = await _roomGrain
            ._dbCtxFactory.CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        BotEntity[] bots = await dbCtx
            .Bots.AsNoTracking()
            .Where(b => b.RoomEntityId == _roomGrain.RoomId.Value && b.DeletedAt == null)
            .ToArrayAsync(ct)
            .ConfigureAwait(true);

        _chatterByBotId.Clear();

        foreach (BotEntity bot in bots)
        {
            string phrases = ReadSkills(bot)
                .GetValueOrDefault(
                    ChatterCommandId.ToString(CultureInfo.InvariantCulture),
                    string.Empty
                );

            _chatterByBotId[bot.Id] =
            [
                .. phrases
                    .Split(ChatterSeparator, StringSplitOptions.RemoveEmptyEntries)
                    .Select(phrase => phrase.Trim())
                    .Where(phrase => phrase.Length > 0),
            ];
        }

        _chatterLoaded = true;
    }

    /// <summary>Drops a bot's cached chatter so the next tick reloads it.</summary>
    private void InvalidateChatter(int botId)
    {
        _ = _chatterByBotId.Remove(botId);
        _ = _nextChatterAtMsByBotId.Remove(botId);
        _chatterLoaded = false;
    }
}
