using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans;
using Turbo.Database.Context;
using Turbo.Database.Entities.Audit;
using Turbo.Database.Entities.Furniture;
using Turbo.Database.Entities.Marketplace;
using Turbo.Database.Entities.Players;
using Turbo.Database.Entities.Room;
using Turbo.Observability.Configuration;
using Turbo.Observability.Metrics;
using Turbo.Observability.Runtime;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Observability;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Orleans.Snapshots.Room;
using Turbo.Primitives.Players.Enums;
using Turbo.Primitives.Rooms.Grains;

namespace Turbo.Dashboard.API.Api;

internal sealed partial class DashboardApiService
{
    public Task<object> AuditAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                int limit = ParseLimit(query["limit"], 50, 500);
                int page = ParsePage(query["page"]);
                int offset = Math.Max(0, (page - 1) * limit);

                IQueryable<AuditEventEntity> q = db.AuditEvents.AsNoTracking();

                DateTime? since = ParseDateTime(query["since"]);
                DateTime? until = ParseDateTime(query["until"]);

                if (since is not null)
                {
                    q = q.Where(a => a.OccurredAt >= since.Value);
                }

                if (until is not null)
                {
                    q = q.Where(a => a.OccurredAt <= until.Value);
                }

                if (int.TryParse(query["actor"], out int actor))
                {
                    q = q.Where(a => a.ActorPlayerId == actor);
                }

                if (int.TryParse(query["target"], out int target))
                {
                    q = q.Where(a => a.TargetPlayerId == target);
                }

                if (
                    Enum.TryParse<AuditCategory>(
                        query["category"],
                        ignoreCase: true,
                        out AuditCategory cat
                    )
                )
                {
                    q = q.Where(a => a.Category == cat);
                }

                string? action = query["action"];
                if (!string.IsNullOrWhiteSpace(action))
                {
                    q = q.Where(a => a.Action == action);
                }

                int total = await q.CountAsync(ct).ConfigureAwait(false);

                var rows = await q.OrderByDescending(a => a.OccurredAt)
                    .Skip(offset)
                    .Take(limit)
                    .Select(a => new
                    {
                        a.Id,
                        a.OccurredAt,
                        category = a.Category.ToString(),
                        a.Action,
                        severity = a.Severity.ToString(),
                        result = a.Result.ToString(),
                        a.ActorPlayerId,
                        a.TargetPlayerId,
                        a.RoomId,
                        a.ItemId,
                        a.IpHash,
                        a.CorrelationId,
                        a.Data,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                List<int> playerIds = NormalizeIds(
                    rows.SelectMany(a => new[] { a.ActorPlayerId, a.TargetPlayerId })
                );

                Dictionary<int, string> playerNames = await LoadPlayerNamesAsync(db, playerIds, ct)
                    .ConfigureAwait(false);

                var rowsWithNames = rows.Select(a => new
                    {
                        a.Id,
                        a.OccurredAt,
                        a.category,
                        a.Action,
                        a.severity,
                        a.result,
                        a.ActorPlayerId,
                        actorName = ResolvePlayerName(playerNames, a.ActorPlayerId),
                        a.TargetPlayerId,
                        targetName = ResolvePlayerName(playerNames, a.TargetPlayerId),
                        a.RoomId,
                        a.ItemId,
                        a.IpHash,
                        a.CorrelationId,
                        a.Data,
                    })
                    .ToList();

                return new
                {
                    count = rows.Count,
                    page,
                    limit,
                    total,
                    offset,
                    items = rowsWithNames,
                };
            },
            ct
        );

    public Task<object> ModerationStatsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                DateTime nowUtc = DateTime.UtcNow;
                DateTime since = ParseDateTime(query["since"]) ?? nowUtc.AddHours(-24);
                DateTime until = ParseDateTime(query["until"]) ?? nowUtc;

                if (since > until)
                {
                    (since, until) = (until, since);
                }

                if (until - since > TimeSpan.FromDays(60))
                {
                    since = until.AddDays(-60);
                }

                int limit = ParseLimit(query["limit"], 80, 500);
                int page = ParsePage(query["page"]);
                int offset = Math.Max(0, (page - 1) * limit);

                IQueryable<AuditEventEntity> q = db
                    .AuditEvents.AsNoTracking()
                    .Where(a => a.Category == AuditCategory.Moderation);

                q = q.Where(a => a.OccurredAt >= since && a.OccurredAt <= until);

                if (int.TryParse(query["actor"], out int actor))
                {
                    q = q.Where(a => a.ActorPlayerId == actor);
                }

                if (int.TryParse(query["target"], out int target))
                {
                    q = q.Where(a => a.TargetPlayerId == target);
                }

                if (int.TryParse(query["room"], out int room))
                {
                    q = q.Where(a => a.RoomId == room);
                }

                string? action = query["action"]?.Trim();
                if (!string.IsNullOrWhiteSpace(action))
                {
                    q = q.Where(a => a.Action == action);
                }

                if (Enum.TryParse<AuditResult>(query["result"], true, out AuditResult resultFilter))
                {
                    q = q.Where(a => a.Result == resultFilter);
                }

                List<ModerationEventRow> events = await q.OrderByDescending(a => a.OccurredAt)
                    .Select(a => new ModerationEventRow(
                        a.Id,
                        a.OccurredAt,
                        a.Action,
                        a.Result.ToString(),
                        a.ActorPlayerId,
                        a.TargetPlayerId,
                        a.RoomId,
                        a.Data,
                        a.CorrelationId
                    ))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                int total = events.Count;
                var rows = events
                    .Skip(offset)
                    .Take(limit)
                    .Select(e => new
                    {
                        e.Id,
                        e.OccurredAt,
                        e.Action,
                        e.Result,
                        e.ActorPlayerId,
                        e.TargetPlayerId,
                        e.RoomId,
                        e.Data,
                        e.CorrelationId,
                    })
                    .ToList();

                List<int> playerIds = NormalizeIds(
                    events.SelectMany(e => new[] { e.ActorPlayerId, e.TargetPlayerId })
                );
                List<int> roomIds = NormalizeIds(events.Select(e => e.RoomId));

                Dictionary<int, string> playerNames = await LoadPlayerNamesAsync(db, playerIds, ct)
                    .ConfigureAwait(false);

                Dictionary<int, string> roomNames = await LoadRoomNamesAsync(db, roomIds, ct)
                    .ConfigureAwait(false);

                HashSet<long> renewedBanEventIds = DetectRenewedBanEventIds(events);

                var rowsWithNames = rows.Select(r =>
                    {
                        ModerationEventRow? raw = events.FirstOrDefault(e =>
                            e.Id == r.Id && e.OccurredAt == r.OccurredAt
                        );
                        int? durationSeconds = raw is null
                            ? null
                            : ParseModerationDurationSeconds(raw.Data);

                        return new
                        {
                            r.Id,
                            r.OccurredAt,
                            r.Action,
                            r.Result,
                            r.ActorPlayerId,
                            actorName = ResolvePlayerName(playerNames, r.ActorPlayerId),
                            r.TargetPlayerId,
                            targetName = ResolvePlayerName(playerNames, r.TargetPlayerId),
                            r.RoomId,
                            roomName = r.RoomId != null
                            && roomNames.TryGetValue(r.RoomId.Value, out string? roomName)
                                ? roomName
                                : null,
                            durationSeconds,
                            duration = FormatModerationDuration(durationSeconds),
                            reason = raw is null
                                ? null
                                : SummarizeModerationReason(r.Action, raw.Data),
                            isRenewal = r.Id != 0 && renewedBanEventIds.Contains(r.Id),
                            r.CorrelationId,
                        };
                    })
                    .ToList();

                var byAction = events
                    .GroupBy(e => e.Action)
                    .Select(g => new { action = g.Key, count = g.Count() })
                    .OrderByDescending(g => g.count)
                    .ThenBy(g => g.action)
                    .ToList();

                var byResult = events
                    .GroupBy(e => e.Result)
                    .Select(g => new { result = g.Key, count = g.Count() })
                    .OrderByDescending(g => g.count)
                    .ThenBy(g => g.result)
                    .ToList();

                var topActors = events
                    .Where(e => e.ActorPlayerId is not null)
                    .GroupBy(e => e.ActorPlayerId!.Value)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .Take(8)
                    .Select(g => new
                    {
                        actorPlayerId = g.Key,
                        actorName = ResolvePlayerName(playerNames, g.Key),
                        count = g.Count(),
                    })
                    .ToList();

                var topTargets = events
                    .Where(e => e.TargetPlayerId is not null)
                    .GroupBy(e => e.TargetPlayerId!.Value)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .Take(8)
                    .Select(g => new
                    {
                        targetPlayerId = g.Key,
                        targetName = ResolvePlayerName(playerNames, g.Key),
                        count = g.Count(),
                    })
                    .ToList();

                var topRooms = events
                    .Where(e => e.RoomId is not null)
                    .GroupBy(e => e.RoomId!.Value)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .Take(8)
                    .Select(g => new
                    {
                        roomId = g.Key,
                        roomName = roomNames.TryGetValue(g.Key, out string? roomName)
                            ? roomName
                            : null,
                        count = g.Count(),
                    })
                    .ToList();

                List<int> durations = events
                    .Select(e => ParseModerationDurationSeconds(e.Data))
                    .Where(seconds => seconds.HasValue && seconds.Value > 0)
                    .Select(seconds => seconds!.Value)
                    .ToList();

                int totalBans = await db
                    .RoomBans.AsNoTracking()
                    .CountAsync(ct)
                    .ConfigureAwait(false);
                int activeBans = await db
                    .RoomBans.AsNoTracking()
                    .Where(b => b.DateExpires > nowUtc)
                    .CountAsync(ct)
                    .ConfigureAwait(false);
                int inactiveBans = totalBans - activeBans;

                TimeSpan bucketSize = ResolveBucketSize(since, until);
                List<ModerationTimelinePoint> timeline = BuildModerationTimeline(
                    events,
                    since,
                    until,
                    bucketSize
                );

                return new
                {
                    window = new { since, until },
                    totals = new
                    {
                        total,
                        limit,
                        page,
                        offset,
                        success = byResult
                            .FirstOrDefault(r => r.result == AuditResult.Success.ToString())
                            ?.count
                            ?? 0,
                        denied = byResult
                            .FirstOrDefault(r => r.result == AuditResult.Denied.ToString())
                            ?.count
                            ?? 0,
                        failed = byResult
                            .FirstOrDefault(r => r.result == AuditResult.Failed.ToString())
                            ?.count
                            ?? 0,
                        retentionRate = totalBans > 0
                            ? Math.Round((double)activeBans / totalBans, 4)
                            : 0d,
                        activeBans,
                        inactiveBans,
                        totalBans,
                        renewalCount = renewedBanEventIds.Count,
                        averageDurationSeconds = durations.Count > 0
                            ? Math.Round(durations.Average(), 2)
                            : 0d,
                    },
                    distribution = new { byAction, byResult },
                    timeline,
                    topActors,
                    topTargets,
                    topRooms,
                    rows = rowsWithNames,
                };
            },
            ct
        );

    private sealed record ModerationEventRow(
        long Id,
        DateTime OccurredAt,
        string Action,
        string Result,
        long? ActorPlayerId,
        long? TargetPlayerId,
        int? RoomId,
        string? Data,
        string? CorrelationId
    );

    private sealed record ModerationTimelinePoint(string Bucket, string Label, int Count);

    private static HashSet<long> DetectRenewedBanEventIds(IReadOnlyList<ModerationEventRow> events)
    {
        if (events.Count == 0)
        {
            return [];
        }

        Dictionary<(long Actor, long Target, int Room), DateTime> lastBanByKey =
            new Dictionary<(long Actor, long Target, int Room), DateTime>();
        HashSet<long> renewed = new HashSet<long>();

        foreach (
            ModerationEventRow evt in events
                .Where(e => e.Action == "moderation.ban")
                .OrderBy(e => e.OccurredAt)
        )
        {
            if (evt.ActorPlayerId is null || evt.TargetPlayerId is null || evt.RoomId is null)
            {
                continue;
            }

            (long, long, int) key = (
                evt.ActorPlayerId.Value,
                evt.TargetPlayerId.Value,
                evt.RoomId.Value
            );

            if (lastBanByKey.TryGetValue(key, out _))
            {
                renewed.Add(evt.Id);
            }

            lastBanByKey[key] = evt.OccurredAt;
        }

        return renewed;
    }

    private static List<ModerationTimelinePoint> BuildModerationTimeline(
        IReadOnlyList<ModerationEventRow> events,
        DateTime since,
        DateTime until,
        TimeSpan bucketSize
    )
    {
        if (events.Count == 0)
        {
            return [];
        }

        Dictionary<DateTime, int> bucketMap = new Dictionary<DateTime, int>();
        DateTime cursor = ResolveTimelineBucket(since, bucketSize);
        DateTime end = ResolveTimelineBucket(until, bucketSize);

        while (cursor <= end)
        {
            bucketMap[cursor] = 0;
            cursor = cursor.Add(bucketSize);
        }

        foreach (ModerationEventRow evt in events)
        {
            DateTime bucket = ResolveTimelineBucket(evt.OccurredAt, bucketSize);
            bucketMap.TryGetValue(bucket, out int count);
            bucketMap[bucket] = count + 1;
        }

        return bucketMap
            .OrderBy(pair => pair.Key)
            .Select(pair => new ModerationTimelinePoint(
                pair.Key.ToString("O"),
                FormatTimelineLabel(pair.Key, bucketSize),
                pair.Value
            ))
            .ToList();
    }

    private static int? ParseModerationDurationSeconds(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return null;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(data);
            if (!doc.RootElement.TryGetProperty("durationSeconds", out JsonElement durationSeconds))
            {
                return null;
            }

            if (
                durationSeconds.ValueKind == JsonValueKind.Number
                && durationSeconds.TryGetInt32(out int parsed)
            )
            {
                return parsed;
            }

            if (
                durationSeconds.ValueKind == JsonValueKind.String
                && int.TryParse(durationSeconds.GetString(), out parsed)
            )
            {
                return parsed;
            }
        }
        catch
        {
            // Intentionally ignore malformed payloads.
        }

        return null;
    }

    private static string? SummarizeModerationReason(string action, string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return null;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(data);

            return action switch
            {
                "moderation.denied"
                    when doc.RootElement.TryGetProperty("action", out JsonElement deniedAction) =>
                    deniedAction.GetString(),
                _ => null,
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? FormatModerationDuration(int? seconds)
    {
        if (seconds is null or <= 0)
        {
            return null;
        }

        int total = Math.Max(0, seconds.Value);
        int minutes = total / 60;
        int hours = minutes / 60;
        int days = hours / 24;

        if (days > 0)
        {
            return $"{days}d {hours % 24}h";
        }

        if (hours > 0)
        {
            return $"{hours}h {minutes % 60}m";
        }

        return $"{minutes}m";
    }
}
