using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Primitives.Moderation;

namespace Vortex.Dashboard.API.Api;

internal sealed partial class DashboardApiService
{
    /// <summary>Read-only overview of the CFH (Call For Help) ticket domain: volume over time,
    /// resolution/sanction rates, top topics, and top reported players — reads straight off
    /// <c>CfhTicketEntity</c>, which is already rich enough on its own (no audit trail needed).
    /// Separate from <see cref="Vortex.Dashboard.API.Operations.DashboardOperationsService.GetCfhQueueAsync"/>,
    /// which drives the live pick/close/release queue; this is analytics, not actionable state.</summary>
    public Task<object> CfhStatsAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                DateTime until = ParseDateTime(query["until"]) ?? DateTime.UtcNow;
                DateTime since = ParseDateTime(query["since"]) ?? until.AddDays(-30);
                string granularity = NormalizeGranularity(query["granularity"]);

                List<(
                    DateTime CreatedAt,
                    CfhTicketState State,
                    DateTime? ClosedAt,
                    CfhTicketCloseReason? CloseReason,
                    bool Sanctioned,
                    int TopicId,
                    int ReportedPlayerEntityId
                )> rows = await db
                    .CfhTickets.AsNoTracking()
                    .Where(t => t.CreatedAt >= since && t.CreatedAt <= until)
                    .Select(t => new ValueTuple<
                        DateTime,
                        CfhTicketState,
                        DateTime?,
                        CfhTicketCloseReason?,
                        bool,
                        int,
                        int
                    >(
                        t.CreatedAt,
                        t.State,
                        t.ClosedAt,
                        t.CloseReason,
                        t.Sanctioned,
                        t.CfhTopicEntityId,
                        t.ReportedPlayerEntityId
                    ))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                int totalTickets = rows.Count;
                int openCount = rows.Count(r => r.State == CfhTicketState.Open);
                int pickedCount = rows.Count(r => r.State == CfhTicketState.Picked);
                int closedCount = rows.Count(r => r.State == CfhTicketState.Closed);
                int sanctionedCount = rows.Count(r => r.Sanctioned);
                double sanctionRate =
                    closedCount > 0 ? Math.Round((double)sanctionedCount / closedCount, 4) : 0d;

                List<double> resolutionMinutes = rows.Where(r => r.ClosedAt is not null)
                    .Select(r => (r.ClosedAt!.Value - r.CreatedAt).TotalMinutes)
                    .ToList();
                double avgResolutionMinutes =
                    resolutionMinutes.Count > 0 ? Math.Round(resolutionMinutes.Average(), 2) : 0d;

                Dictionary<DateTime, int> bucketMap = new();
                DateTime cursor = ResolveCalendarBucket(since, granularity);
                DateTime end = ResolveCalendarBucket(until, granularity);

                while (cursor <= end)
                {
                    bucketMap[cursor] = 0;
                    cursor = NextCalendarBucket(cursor, granularity);
                }

                foreach (
                    (
                        DateTime CreatedAt,
                        CfhTicketState State,
                        DateTime? ClosedAt,
                        CfhTicketCloseReason? CloseReason,
                        bool Sanctioned,
                        int TopicId,
                        int ReportedPlayerEntityId
                    ) row in rows
                )
                {
                    DateTime bucket = ResolveCalendarBucket(row.CreatedAt, granularity);
                    bucketMap[bucket] = bucketMap.GetValueOrDefault(bucket) + 1;
                }

                var timeline = bucketMap
                    .OrderBy(pair => pair.Key)
                    .Select(pair => new
                    {
                        bucket = pair.Key.ToString("O"),
                        label = FormatCalendarLabel(pair.Key, granularity),
                        ticketsCreated = pair.Value,
                    })
                    .ToList();

                var byCloseReason = rows.Where(r => r.CloseReason is not null)
                    .GroupBy(r => r.CloseReason!.Value)
                    .Select(g => new { reason = g.Key.ToString(), count = g.Count() })
                    .OrderByDescending(g => g.count)
                    .ToList();

                var topTopics = await (
                    from t in db.CfhTickets.AsNoTracking()
                    where t.CreatedAt >= since && t.CreatedAt <= until
                    group t by t.CfhTopicEntityId into g
                    orderby g.Count() descending
                    select new { topicId = g.Key, count = g.Count() }
                )
                    .Take(10)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                Dictionary<int, string> topicNames = await db
                    .CfhTopics.AsNoTracking()
                    .Where(topic => topTopics.Select(t => t.topicId).Contains(topic.Id))
                    .ToDictionaryAsync(topic => topic.Id, topic => topic.Name, ct)
                    .ConfigureAwait(false);

                var topTopicsWithNames = topTopics
                    .Select(t => new
                    {
                        t.topicId,
                        topicName = topicNames.GetValueOrDefault(t.topicId, $"topic #{t.topicId}"),
                        t.count,
                    })
                    .ToList();

                var topReported = rows.GroupBy(r => r.ReportedPlayerEntityId)
                    .Select(g => new { playerId = g.Key, reportCount = g.Count() })
                    .OrderByDescending(g => g.reportCount)
                    .Take(10)
                    .ToList();

                List<int> reportedIds = NormalizeIds(topReported.Select(r => (int?)r.playerId));
                Dictionary<int, string> reportedNames = await LoadPlayerNamesAsync(
                        db,
                        reportedIds,
                        ct
                    )
                    .ConfigureAwait(false);

                var topReportedWithNames = topReported
                    .Select(r => new
                    {
                        r.playerId,
                        playerName = ResolvePlayerName(reportedNames, (int?)r.playerId),
                        r.reportCount,
                    })
                    .ToList();

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
                        totalTickets,
                        openCount,
                        pickedCount,
                        closedCount,
                        sanctionedCount,
                        sanctionRate,
                        avgResolutionMinutes,
                    },
                    timeline,
                    byCloseReason,
                    topTopics = topTopicsWithNames,
                    topReportedPlayers = topReportedWithNames,
                };
            },
            ct
        );
}
