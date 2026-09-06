using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Bots;

namespace Vortex.Dashboard.API.Api;

/// <summary>
/// Read surface for the room actors that are neither players nor furni: bots and the hand items
/// they (and players) pass around.
/// <para>
/// A bot's configuration lives in one <c>skills_json</c> blob keyed by the client's own skill ids,
/// so the raw row says nothing an operator can read. Every listing here decodes it: which buttons
/// the bot's menu will actually show, and — for the chatter skill, whose payload is the client's
/// <c>;#;</c> blob — how many phrases it has and whether it speaks on its own.
/// </para>
/// </summary>
internal sealed partial class DashboardApiService
{
    /// <summary>Paginated bot roster. Filters: <c>q</c> (name/motto), <c>ownerId</c>, <c>roomId</c>,
    /// and <c>placed</c> (true = standing in a room, false = in its owner's hand).</summary>
    public Task<object> BotsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                string term = (query["q"] ?? string.Empty).Trim();
                int limit = ParseLimit(query["limit"], 40, 200);
                int page = ParsePage(query["page"]);
                int offset = Math.Max(0, (page - 1) * limit);

                IQueryable<BotEntity> bots = db.Bots.AsNoTracking();

                if (term.Length > 0)
                {
                    bots = bots.Where(b => b.Name.Contains(term) || b.Motto.Contains(term));
                }

                if (int.TryParse(query["ownerId"], out int ownerId) && ownerId > 0)
                {
                    bots = bots.Where(b => b.OwnerPlayerEntityId == ownerId);
                }

                if (int.TryParse(query["roomId"], out int roomId) && roomId > 0)
                {
                    bots = bots.Where(b => b.RoomEntityId == roomId);
                }

                if (bool.TryParse(query["placed"], out bool placed))
                {
                    bots = placed
                        ? bots.Where(b => b.RoomEntityId != null)
                        : bots.Where(b => b.RoomEntityId == null);
                }

                int total = await bots.CountAsync(ct).ConfigureAwait(false);

                var rows = await bots.OrderByDescending(b => b.Id)
                    .Skip(offset)
                    .Take(limit)
                    .Select(b => new
                    {
                        b.Id,
                        b.Name,
                        b.Motto,
                        b.Figure,
                        gender = b.Gender.ToString(),
                        b.OwnerPlayerEntityId,
                        b.RoomEntityId,
                        b.X,
                        b.Y,
                        b.Z,
                        rotation = (int)b.Rotation,
                        b.SkillsJson,
                        b.CreatedAt,
                        b.UpdatedAt,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<int, string> ownerNames = await LoadPlayerNamesAsync(
                        db,
                        NormalizeIds(rows.Select(b => (int?)b.OwnerPlayerEntityId)),
                        ct
                    )
                    .ConfigureAwait(false);

                Dictionary<int, string> roomNames = await LoadRoomNamesAsync(
                        db,
                        NormalizeIds(rows.Select(b => b.RoomEntityId)),
                        ct
                    )
                    .ConfigureAwait(false);

                var items = rows.Select(b =>
                    {
                        BotSkillSummary skills = SummarizeBotSkills(b.SkillsJson);

                        return new
                        {
                            b.Id,
                            b.Name,
                            b.Motto,
                            b.Figure,
                            avatarUrl = _assetUrls.AvatarImage(b.Figure),
                            b.gender,
                            ownerId = b.OwnerPlayerEntityId,
                            ownerName = ResolvePlayerName(ownerNames, b.OwnerPlayerEntityId),
                            roomId = b.RoomEntityId,
                            roomName = b.RoomEntityId is { } id
                                ? roomNames.GetValueOrDefault(id)
                                : null,
                            placed = b.RoomEntityId is not null,
                            b.X,
                            b.Y,
                            b.Z,
                            b.rotation,
                            skills = skills.SkillIds,
                            skillNames = skills.SkillNames,
                            phraseCount = skills.PhraseCount,
                            autoChat = skills.AutoChat,
                            chatDelaySeconds = skills.DelaySeconds,
                            wanders = skills.Wanders,
                            dances = skills.Dances,
                            b.CreatedAt,
                            b.UpdatedAt,
                        };
                    })
                    .ToList();

                return new
                {
                    page,
                    limit,
                    offset,
                    total,
                    count = items.Count,
                    items,
                };
            },
            ct
        );

    /// <summary>One bot with its decoded chatter phrases and its raw skill blob — the raw blob is
    /// kept because a configuration the decoder does not understand is exactly what an operator
    /// investigating a silent bot needs to see.</summary>
    public Task<object?> BotDetailAsync(int botId, CancellationToken ct) =>
        QueryAsync<object?>(
            async db =>
            {
                BotEntity? bot = await db
                    .Bots.AsNoTracking()
                    .FirstOrDefaultAsync(b => b.Id == botId, ct)
                    .ConfigureAwait(false);

                if (bot is null)
                {
                    return null;
                }

                Dictionary<int, string> ownerNames = await LoadPlayerNamesAsync(
                        db,
                        [bot.OwnerPlayerEntityId],
                        ct
                    )
                    .ConfigureAwait(false);

                Dictionary<int, string> roomNames = await LoadRoomNamesAsync(
                        db,
                        NormalizeIds([bot.RoomEntityId]),
                        ct
                    )
                    .ConfigureAwait(false);

                BotSkillSummary skills = SummarizeBotSkills(bot.SkillsJson);

                return new
                {
                    bot.Id,
                    bot.Name,
                    bot.Motto,
                    bot.Figure,
                    avatarUrl = _assetUrls.AvatarImage(bot.Figure),
                    gender = bot.Gender.ToString(),
                    ownerId = bot.OwnerPlayerEntityId,
                    ownerName = ResolvePlayerName(ownerNames, bot.OwnerPlayerEntityId),
                    roomId = bot.RoomEntityId,
                    roomName = bot.RoomEntityId is { } id ? roomNames.GetValueOrDefault(id) : null,
                    placed = bot.RoomEntityId is not null,
                    bot.X,
                    bot.Y,
                    bot.Z,
                    rotation = (int)bot.Rotation,
                    skills = skills.SkillIds,
                    skillNames = skills.SkillNames,
                    phrases = skills.Phrases,
                    autoChat = skills.AutoChat,
                    chatDelaySeconds = skills.DelaySeconds,
                    mixSentences = skills.Markov,
                    wanders = skills.Wanders,
                    dances = skills.Dances,
                    rawSkillsJson = bot.SkillsJson,
                    bot.CreatedAt,
                    bot.UpdatedAt,
                };
            },
            ct
        );

    /// <summary>Bot population health: how many exist, how many are actually standing in a room,
    /// how many were ever configured to say anything, and who owns them.</summary>
    public Task<object> BotsStatsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                (DateTime since, DateTime until) = ResolveWindow(query, DateTime.UtcNow);
                string granularity = NormalizeGranularity(query["granularity"]);

                List<BotStatsRow> rows = await db
                    .Bots.AsNoTracking()
                    .Select(b => new BotStatsRow(
                        b.CreatedAt,
                        b.RoomEntityId,
                        b.OwnerPlayerEntityId,
                        b.Gender.ToString(),
                        b.SkillsJson
                    ))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<BotSkillSummary> summaries = rows.ConvertAll(r =>
                    SummarizeBotSkills(r.SkillsJson)
                );

                int totalBots = rows.Count;
                int placedBots = rows.Count(r => r.RoomId is not null);
                int configuredBots = summaries.Count(s => s.SkillIds.Count > 0);
                int chattyBots = summaries.Count(s => s.PhraseCount > 0);
                int autoChatBots = summaries.Count(s => s.AutoChat);
                int wanderingBots = summaries.Count(s => s.Wanders);
                int dancingBots = summaries.Count(s => s.Dances);

                int distinctOwners = rows.Select(r => r.OwnerId).Distinct().Count();
                int roomsWithBots = rows.Where(r => r.RoomId is not null)
                    .Select(r => r.RoomId)
                    .Distinct()
                    .Count();

                var byGender = rows.GroupBy(r => r.Gender)
                    .Select(g => new { gender = g.Key, count = g.Count() })
                    .OrderByDescending(g => g.count)
                    .ToList();

                Dictionary<DateTime, int> bucketMap = new();
                DateTime cursor = ResolveCalendarBucket(since, granularity);
                DateTime end = ResolveCalendarBucket(until, granularity);

                while (cursor <= end)
                {
                    bucketMap[cursor] = 0;
                    cursor = NextCalendarBucket(cursor, granularity);
                }

                foreach (
                    BotStatsRow row in rows.Where(r => r.CreatedAt >= since && r.CreatedAt <= until)
                )
                {
                    DateTime bucket = ResolveCalendarBucket(row.CreatedAt, granularity);
                    bucketMap[bucket] = bucketMap.GetValueOrDefault(bucket) + 1;
                }

                var growth = bucketMap
                    .OrderBy(pair => pair.Key)
                    .Select(pair => new
                    {
                        bucket = pair.Key.ToString("O"),
                        label = FormatCalendarLabel(pair.Key, granularity),
                        botsCreated = pair.Value,
                    })
                    .ToList();

                var topOwnerRows = rows.GroupBy(r => r.OwnerId)
                    .Select(g => new { ownerId = g.Key, botCount = g.Count() })
                    .OrderByDescending(g => g.botCount)
                    .Take(10)
                    .ToList();

                Dictionary<int, string> ownerNames = await LoadPlayerNamesAsync(
                        db,
                        NormalizeIds(topOwnerRows.Select(o => (int?)o.ownerId)),
                        ct
                    )
                    .ConfigureAwait(false);

                var topRoomRows = rows.Where(r => r.RoomId is not null)
                    .GroupBy(r => r.RoomId!.Value)
                    .Select(g => new { roomId = g.Key, botCount = g.Count() })
                    .OrderByDescending(g => g.botCount)
                    .Take(10)
                    .ToList();

                Dictionary<int, string> roomNames = await LoadRoomNamesAsync(
                        db,
                        NormalizeIds(topRoomRows.Select(r => (int?)r.roomId)),
                        ct
                    )
                    .ConfigureAwait(false);

                return new
                {
                    window = new
                    {
                        since,
                        until,
                        granularity,
                    },
                    totals = new
                    {
                        totalBots,
                        placedBots,
                        inventoryBots = totalBots - placedBots,
                        configuredBots,
                        chattyBots,
                        autoChatBots,
                        wanderingBots,
                        dancingBots,
                        distinctOwners,
                        roomsWithBots,
                    },
                    byGender,
                    growth,
                    topOwners = topOwnerRows
                        .Select(o => new
                        {
                            o.ownerId,
                            ownerName = ResolvePlayerName(ownerNames, o.ownerId),
                            o.botCount,
                        })
                        .ToList(),
                    topRooms = topRoomRows
                        .Select(r => new
                        {
                            r.roomId,
                            roomName = roomNames.GetValueOrDefault(r.roomId),
                            r.botCount,
                        })
                        .ToList(),
                };
            },
            ct
        );

    /// <summary>The hand-item table: what a pet gets out of each id. Rows only exist for consumables,
    /// so an id missing here is held and passed around but never eaten — which is correct for a
    /// camera and a bug for a plate of food, and the reason the whole table is shown at once.</summary>
    public Task<object> HandItemsAsync(CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                var rows = await db
                    .HandItems.AsNoTracking()
                    .OrderBy(h => h.HandItemId)
                    .Select(h => new
                    {
                        h.Id,
                        h.HandItemId,
                        h.Name,
                        h.Nutrition,
                        h.Thirst,
                        consumable = h.Nutrition > 0 || h.Thirst > 0,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                // A hand item has no icon of its own anywhere in the client: the only picture of one
                // is an avatar holding it, which is what this renders.
                var items = rows.Select(h => new
                    {
                        h.Id,
                        h.HandItemId,
                        h.Name,
                        h.Nutrition,
                        h.Thirst,
                        h.consumable,
                        imageUrl = _assetUrls.HandItemImage(h.HandItemId),
                    })
                    .ToList();

                return new
                {
                    count = items.Count,
                    consumableCount = items.Count(i => i.consumable),
                    // Lets the editor preview an id that has no row yet -- which is every id being
                    // added for the first time.
                    imageTemplate = _assetUrls.HandItemImageTemplate,
                    items,
                };
            },
            ct
        );

    /// <summary>Decodes <c>skills_json</c> (client skill id → payload) into something readable. A
    /// blob that will not parse yields an empty summary rather than throwing: one malformed bot must
    /// not take the whole listing down.</summary>
    private static BotSkillSummary SummarizeBotSkills(string? skillsJson)
    {
        if (string.IsNullOrWhiteSpace(skillsJson))
        {
            return BotSkillSummary.Empty;
        }

        Dictionary<string, string?>? payload;
        try
        {
            payload = JsonSerializer.Deserialize<Dictionary<string, string?>>(skillsJson);
        }
        catch (JsonException)
        {
            return BotSkillSummary.Empty with { Malformed = true };
        }

        if (payload is null || payload.Count == 0)
        {
            return BotSkillSummary.Empty;
        }

        List<int> skillIds = [];
        string? chatterData = null;

        foreach ((string key, string? value) in payload)
        {
            if (!int.TryParse(key, out int skillId))
            {
                continue;
            }

            skillIds.Add(skillId);

            if (skillId == BotSkillId.Chatter)
            {
                chatterData = value;
            }
        }

        skillIds.Sort();
        BotChatterConfiguration chatter = BotChatterConfiguration.Parse(chatterData);

        return new BotSkillSummary
        {
            SkillIds = skillIds,
            SkillNames = skillIds.ConvertAll(DescribeBotSkill),
            Phrases = [.. chatter.Phrases],
            PhraseCount = chatter.Phrases.Length,
            AutoChat = chatter.AutoChat,
            DelaySeconds = chatter.DelaySeconds,
            Markov = chatter.Markov,
            Wanders = skillIds.Contains(BotSkillId.RandomWalk),
            Dances = skillIds.Contains(BotSkillId.Dance),
        };
    }

    private static string DescribeBotSkill(int skillId) =>
        skillId switch
        {
            BotSkillId.DressUp => "dressUp",
            BotSkillId.Chatter => "chatter",
            BotSkillId.RandomWalk => "randomWalk",
            BotSkillId.Dance => "dance",
            BotSkillId.ChangeName => "changeName",
            BotSkillId.NoPickUp => "noPickUp",
            _ => $"skill{skillId}",
        };

    private sealed record BotStatsRow(
        DateTime CreatedAt,
        int? RoomId,
        int OwnerId,
        string Gender,
        string? SkillsJson
    );

    private sealed record BotSkillSummary
    {
        public static BotSkillSummary Empty { get; } = new();

        public List<int> SkillIds { get; init; } = [];
        public List<string> SkillNames { get; init; } = [];
        public List<string> Phrases { get; init; } = [];
        public int PhraseCount { get; init; }
        public bool AutoChat { get; init; }
        public int DelaySeconds { get; init; }
        public bool Markov { get; init; }
        public bool Wanders { get; init; }
        public bool Dances { get; init; }
        public bool Malformed { get; init; }
    }
}
