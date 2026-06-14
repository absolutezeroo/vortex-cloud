using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Turbo.Database.Context;
using Turbo.Observability.Configuration;
using Turbo.Observability.Diagnostics;
using Turbo.Primitives.Observability;

namespace Turbo.Observability.Dashboard;

/// <summary>
/// Native admin dashboard: a small, isolated, read-only HTTP API over the durable audit tables. It
/// runs on its own <see cref="HttpListener"/> (no ASP.NET dependency, no coupling to the game socket
/// host), binds to localhost by default, and requires a shared token on every request. It performs
/// only SELECTs — there is no route that mutates audit data, so staff cannot erase their own trail.
/// </summary>
public sealed class AdminApiService(
    IOptions<ObservabilityConfig> options,
    IDbContextFactory<TurboDbContext> dbContextFactory,
    IAuditSink auditSink,
    ILogger<AdminApiService> logger
) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ObservabilityConfig _config = options.Value;
    private readonly IDbContextFactory<TurboDbContext> _dbContextFactory = dbContextFactory;
    private readonly IAuditSink _auditSink = auditSink;
    private readonly ILogger<AdminApiService> _logger = logger;
    private DateTime _startedAtUtc;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.DashboardEnabled)
            return;

        if (string.IsNullOrWhiteSpace(_config.DashboardToken))
        {
            _logger.LogWarning(
                TurboEventIds.DashboardDisabled,
                "Turbo dashboard is enabled but no DashboardToken is set; refusing to start to avoid anonymous admin access."
            );
            return;
        }

        _startedAtUtc = DateTime.UtcNow;

        using var listener = new HttpListener();
        var prefix = $"http://{_config.DashboardHost}:{_config.DashboardPort}/";
        listener.Prefixes.Add(prefix);

        try
        {
            listener.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                TurboEventIds.DashboardFault,
                ex,
                "Failed to start Turbo dashboard on {Prefix}",
                prefix
            );
            return;
        }

        using var reg = stoppingToken.Register(listener.Stop);
        _logger.LogInformation(
            TurboEventIds.DashboardReady,
            "Turbo dashboard listening on {Prefix}",
            prefix
        );

        while (!stoppingToken.IsCancellationRequested)
        {
            HttpListenerContext ctx;

            try
            {
                ctx = await listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (HttpListenerException)
            {
                break;
            }

            _ = HandleRequestAsync(ctx, stoppingToken);
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        try
        {
            var path = ctx.Request.Url?.AbsolutePath ?? "/";
            var query = ctx.Request.QueryString;

            var token = ctx.Request.Headers["X-Admin-Token"] ?? query["token"];

            if (!TokenMatches(token))
            {
                EmitDashboardAudit(path, AuditResult.Denied, 401, "Unauthorized");

                await WriteJsonAsync(ctx, 401, new { error = "unauthorized" }, ct)
                    .ConfigureAwait(false);
                return;
            }

            if (path is "/" or "/index.html")
            {
                EmitDashboardAudit(path, AuditResult.Success, 200, "DashboardHtml");
                await WriteHtmlAsync(ctx, ct).ConfigureAwait(false);
                return;
            }

            object? payload = path switch
            {
                "/api/overview" => await OverviewAsync(ct).ConfigureAwait(false),
                "/api/audit" => await AuditAsync(query, ct).ConfigureAwait(false),
                "/api/economy" => await EconomyAsync(query, ct).ConfigureAwait(false),
                "/api/search" => await SearchAsync(query, ct).ConfigureAwait(false),
                _ when path.StartsWith("/api/item/", StringComparison.Ordinal) => await ItemAsync(
                        path["/api/item/".Length..],
                        query,
                        ct
                    )
                    .ConfigureAwait(false),
                _ => null,
            };

            if (payload is null)
            {
                EmitDashboardAudit(path, AuditResult.Failed, 404, "NotFound");

                await WriteJsonAsync(ctx, 404, new { error = "not_found" }, ct)
                    .ConfigureAwait(false);
                return;
            }

            EmitDashboardAudit(path, AuditResult.Success, 200, "DataResponse");

            await WriteJsonAsync(ctx, 200, payload, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(TurboEventIds.DashboardFault, ex, "Turbo dashboard request failed");
            EmitDashboardAudit(
                ctx.Request.Url?.AbsolutePath ?? "/",
                AuditResult.Failed,
                500,
                "InternalError"
            );

            try
            {
                await WriteJsonAsync(ctx, 500, new { error = "internal" }, ct)
                    .ConfigureAwait(false);
            }
            catch
            {
                // best effort
            }
        }
    }

    private void EmitDashboardAudit(
        string path,
        AuditResult result,
        int status,
        string eventKind
    )
    {
        _auditSink.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Security,
                Action = "audit.viewed",
                Severity = result == AuditResult.Success ? AuditSeverity.Info : AuditSeverity.Warning,
                Result = result,
                IpHash = null,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        path,
                        status,
                        kind = eventKind,
                    }
                ),
            }
        );
    }

    private Task<object> OverviewAsync(CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                var since = DateTime.UtcNow.AddHours(-1);

                var byCategory = await db
                    .AuditEvents.AsNoTracking()
                    .Where(a => a.OccurredAt >= since)
                    .GroupBy(a => a.Category)
                    .Select(g => new { category = g.Key.ToString(), count = g.Count() })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                return new
                {
                    status = "ok",
                    uptimeSeconds = (long)(DateTime.UtcNow - _startedAtUtc).TotalSeconds,
                    managedMemoryMb = GC.GetTotalMemory(false) / 1024 / 1024,
                    auditLastHourByCategory = byCategory,
                    totals = new
                    {
                        audit = await db.AuditEvents.CountAsync(ct).ConfigureAwait(false),
                        ledger = await db.EconomyLedger.CountAsync(ct).ConfigureAwait(false),
                        items = await db.ItemEvents.CountAsync(ct).ConfigureAwait(false),
                    },
                };
            },
            ct
        );

    private Task<object> AuditAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                var limit = ParseLimit(query["limit"], 50, 500);
                var page = ParsePage(query["page"]);
                var offset = Math.Max(0, (page - 1) * limit);

                var q = db.AuditEvents.AsNoTracking();

                var since = ParseDateTime(query["since"]);
                var until = ParseDateTime(query["until"]);

                if (since is not null)
                    q = q.Where(a => a.OccurredAt >= since.Value);

                if (until is not null)
                    q = q.Where(a => a.OccurredAt <= until.Value);

                if (int.TryParse(query["actor"], out var actor))
                    q = q.Where(a => a.ActorPlayerId == actor);

                if (int.TryParse(query["target"], out var target))
                    q = q.Where(a => a.TargetPlayerId == target);

                if (Enum.TryParse<AuditCategory>(query["category"], ignoreCase: true, out var cat))
                    q = q.Where(a => a.Category == cat);

                var action = query["action"];
                if (!string.IsNullOrWhiteSpace(action))
                    q = q.Where(a => a.Action == action);

                var total = await q.CountAsync(ct).ConfigureAwait(false);

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

                return new
                {
                    count = rows.Count,
                    page,
                    limit,
                    total,
                    offset,
                    items = rows,
                };
            },
            ct
        );

    private Task<object> EconomyAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                var limit = ParseLimit(query["limit"], 50, 500);
                var page = ParsePage(query["page"]);
                var offset = Math.Max(0, (page - 1) * limit);
                var q = db.EconomyLedger.AsNoTracking();

                var since = ParseDateTime(query["since"]);
                var until = ParseDateTime(query["until"]);

                if (since is not null)
                    q = q.Where(l => l.OccurredAt >= since.Value);

                if (until is not null)
                    q = q.Where(l => l.OccurredAt <= until.Value);

                if (int.TryParse(query["player"], out var player))
                    q = q.Where(l => l.PlayerId == player);

                var total = await q.CountAsync(ct).ConfigureAwait(false);

                var rows = await q.OrderByDescending(l => l.OccurredAt)
                    .Skip(offset)
                    .Take(limit)
                    .Select(l => new
                    {
                        l.Id,
                        l.OccurredAt,
                        l.PlayerId,
                        l.Currency,
                        l.ActivityPointType,
                        l.Delta,
                        l.BalanceAfter,
                        reason = l.Reason.ToString(),
                        l.RefId,
                        l.CorrelationId,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                return new
                {
                    count = rows.Count,
                    page,
                    limit,
                    total,
                    offset,
                    items = rows,
                };
            },
            ct
        );

    private Task<object?> ItemAsync(string idText, NameValueCollection query, CancellationToken ct)
    {
        if (!long.TryParse(idText, out var itemId))
            return Task.FromResult<object?>(null);

        return QueryAsync<object?>(
            async db =>
            {
                var limit = ParseLimit(query["limit"], 50, 500);
                var page = ParsePage(query["page"]);
                var offset = Math.Max(0, (page - 1) * limit);
                var q = db.ItemEvents.AsNoTracking().Where(i => i.ItemId == itemId);

                var since = ParseDateTime(query["since"]);
                var until = ParseDateTime(query["until"]);

                if (since is not null)
                    q = q.Where(i => i.OccurredAt >= since.Value);

                if (until is not null)
                    q = q.Where(i => i.OccurredAt <= until.Value);

                var total = await q.CountAsync(ct).ConfigureAwait(false);

                var rows = await q
                    .OrderBy(i => i.OccurredAt)
                    .Skip(offset)
                    .Take(limit)
                    .Select(i => new
                    {
                        i.Id,
                        i.OccurredAt,
                        eventType = i.EventType.ToString(),
                        i.ActorPlayerId,
                        i.FromOwnerId,
                        i.ToOwnerId,
                        i.RoomId,
                        i.CorrelationId,
                        i.Data,
                    })
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                return new
                {
                    itemId,
                    page,
                    limit,
                    total,
                    offset,
                    count = rows.Count,
                    history = rows,
                };
            },
            ct
        );
    }

    private Task<object> SearchAsync(NameValueCollection query, CancellationToken ct) =>
        QueryAsync<object>(
            async db =>
            {
                var term = (query["q"] ?? string.Empty).Trim();
                var limit = ParseLimit(query["limit"], 50, 500);
                var page = ParsePage(query["page"]);
                var offset = Math.Max(0, (page - 1) * limit);

                var since = ParseDateTime(query["since"]);
                var until = ParseDateTime(query["until"]);

                // Correlation id: 32 hex chars (Guid "N").
                if (term.Length == 32 && term.All(Uri.IsHexDigit))
                {
                    var audit = db
                        .AuditEvents.AsNoTracking()
                        .Where(a => a.CorrelationId == term);

                    if (since is not null)
                        audit = audit.Where(a => a.OccurredAt >= since.Value);

                    if (until is not null)
                        audit = audit.Where(a => a.OccurredAt <= until.Value);

                    var ledger = db
                        .EconomyLedger.AsNoTracking()
                        .Where(l => l.CorrelationId == term);

                    if (since is not null)
                        ledger = ledger.Where(l => l.OccurredAt >= since.Value);

                    if (until is not null)
                        ledger = ledger.Where(l => l.OccurredAt <= until.Value);

                    var items = db
                        .ItemEvents.AsNoTracking()
                        .Where(i => i.CorrelationId == term);

                    if (since is not null)
                        items = items.Where(i => i.OccurredAt >= since.Value);

                    if (until is not null)
                        items = items.Where(i => i.OccurredAt <= until.Value);

                    var auditRows = await audit
                        .OrderBy(a => a.OccurredAt)
                        .Skip(offset)
                        .Take(limit)
                        .Select(a => new
                        {
                            a.OccurredAt,
                            category = a.Category.ToString(),
                            a.Action,
                            a.ActorPlayerId,
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false);

                    var ledgerRows = await ledger
                        .OrderBy(l => l.OccurredAt)
                        .Skip(offset)
                        .Take(limit)
                        .Select(l => new
                        {
                            l.OccurredAt,
                            l.PlayerId,
                            l.Currency,
                            l.Delta,
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false);

                    var itemRows = await items
                        .OrderBy(i => i.OccurredAt)
                        .Skip(offset)
                        .Take(limit)
                        .Select(i => new
                        {
                            i.OccurredAt,
                            i.ItemId,
                            eventType = i.EventType.ToString(),
                        })
                        .ToListAsync(ct)
                        .ConfigureAwait(false);

                    return new
                    {
                        kind = "correlationId",
                        term,
                        page,
                        limit,
                        offset,
                        auditTotal = await audit.CountAsync(ct).ConfigureAwait(false),
                        ledgerTotal = await ledger.CountAsync(ct).ConfigureAwait(false),
                        itemTotal = await items.CountAsync(ct).ConfigureAwait(false),
                        audit = auditRows,
                        ledger = ledgerRows,
                        items = itemRows,
                    };
                }

                if (int.TryParse(term, out var id))
                {
                    var audit = db
                        .AuditEvents.AsNoTracking()
                        .Where(a => a.ActorPlayerId == id || a.TargetPlayerId == id);

                    if (since is not null)
                        audit = audit.Where(a => a.OccurredAt >= since.Value);

                    if (until is not null)
                        audit = audit.Where(a => a.OccurredAt <= until.Value);

                    var ledger = db
                        .EconomyLedger.AsNoTracking()
                        .Where(l => l.PlayerId == id);

                    if (since is not null)
                        ledger = ledger.Where(l => l.OccurredAt >= since.Value);

                    if (until is not null)
                        ledger = ledger.Where(l => l.OccurredAt <= until.Value);

                    var itemHistory = db
                        .ItemEvents.AsNoTracking()
                        .Where(i => i.ItemId == id);

                    if (since is not null)
                        itemHistory = itemHistory.Where(i => i.OccurredAt >= since.Value);

                    if (until is not null)
                        itemHistory = itemHistory.Where(i => i.OccurredAt <= until.Value);

                    return new
                    {
                        kind = "id",
                        term,
                        page,
                        limit,
                        offset,
                        asActor = await audit
                            .OrderByDescending(a => a.OccurredAt)
                            .Skip(offset)
                            .Take(limit)
                            .Select(a => new
                            {
                                a.OccurredAt,
                                category = a.Category.ToString(),
                                a.Action,
                                a.ActorPlayerId,
                                a.TargetPlayerId,
                            })
                            .ToListAsync(ct)
                            .ConfigureAwait(false),
                        auditTotal = await audit.CountAsync(ct).ConfigureAwait(false),
                        ledger = await ledger
                            .OrderByDescending(l => l.OccurredAt)
                            .Skip(offset)
                            .Take(limit)
                            .Select(l => new
                            {
                                l.OccurredAt,
                                l.Currency,
                                l.Delta,
                                l.BalanceAfter,
                            })
                            .ToListAsync(ct)
                            .ConfigureAwait(false),
                        ledgerTotal = await ledger.CountAsync(ct).ConfigureAwait(false),
                        itemHistory = await itemHistory
                            .OrderBy(i => i.OccurredAt)
                            .Skip(offset)
                            .Take(limit)
                            .Select(i => new
                            {
                                i.OccurredAt,
                                eventType = i.EventType.ToString(),
                            })
                            .ToListAsync(ct)
                            .ConfigureAwait(false),
                        itemTotal = await itemHistory.CountAsync(ct).ConfigureAwait(false),
                    };
                }

                return new
                {
                    kind = "unknown",
                    term,
                    hint = "Enter a player/item id, or a 32-char correlation id.",
                };
            },
            ct
        );

    private async Task<T> QueryAsync<T>(Func<TurboDbContext, Task<T>> work, CancellationToken ct)
    {
        var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            return await work(db).ConfigureAwait(false);
        }
        finally
        {
            await db.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task WriteJsonAsync(
        HttpListenerContext ctx,
        int status,
        object payload,
        CancellationToken ct
    )
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, Json);
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes, ct).ConfigureAwait(false);
        ctx.Response.Close();
    }

    private static async Task WriteHtmlAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(DashboardHtml.Page);
        ctx.Response.StatusCode = 200;
        ctx.Response.ContentType = "text/html; charset=utf-8";
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes, ct).ConfigureAwait(false);
        ctx.Response.Close();
    }

    private bool TokenMatches(string? provided)
    {
        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(_config.DashboardToken));
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(provided ?? string.Empty));
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private static int ParseLimit(string? value, int fallback, int max) =>
        int.TryParse(value, out var n) ? Math.Clamp(n, 1, max) : fallback;

    private static int ParsePage(string? value)
    {
        if (!int.TryParse(value, out var page))
            return 1;

        return Math.Max(1, page);
    }

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTimeOffset.TryParse(value, out var parsedOffset))
            return parsedOffset.UtcDateTime;

        if (DateTime.TryParse(value, out var parsedDate))
            return parsedDate;

        return null;
    }

}

