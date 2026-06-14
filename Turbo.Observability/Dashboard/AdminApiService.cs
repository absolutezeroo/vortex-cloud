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
using Orleans;
using Turbo.Database.Context;
using Turbo.Observability.Configuration;
using Turbo.Observability.Diagnostics;
using Turbo.Observability.Runtime;
using Turbo.Primitives.Networking;
using Turbo.Primitives.Observability;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Rooms.Grains;

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
    IGrainFactory grainFactory,
    ISessionGateway sessionGateway,
    ILiveStatsAggregator liveStats,
    IIncidentDetectionService incidentDetection,
    IInfrastructureHealthService infrastructureHealth,
    ILogger<AdminApiService> logger
) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    private const string HtmlContentType = "text/html; charset=utf-8";
    private const string CssContentType = "text/css; charset=utf-8";
    private const string JsonContentType = "application/json; charset=utf-8";
    private const string JsContentType = "application/javascript; charset=utf-8";

    private readonly ObservabilityConfig _config = options.Value;
    private readonly IDbContextFactory<TurboDbContext> _dbContextFactory = dbContextFactory;
    private readonly IAuditSink _auditSink = auditSink;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ISessionGateway _sessionGateway = sessionGateway;
    private readonly ILiveStatsAggregator _liveStats = liveStats;
    private readonly IIncidentDetectionService _incidentDetection = incidentDetection;
    private readonly IInfrastructureHealthService _infrastructureHealth = infrastructureHealth;
    private readonly ILogger<AdminApiService> _logger = logger;
    private DateTime _startedAtUtc;
    private bool UseLegacyFallbackTokens =>
        string.IsNullOrWhiteSpace(_config.DashboardAdminToken)
        && string.IsNullOrWhiteSpace(_config.DashboardEconomyToken)
        && string.IsNullOrWhiteSpace(_config.DashboardModeratorToken);

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
            var role = ResolveDashboardRole(token);

            if (path.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase))
            {
                var asset = path["/assets/".Length..];

                if (!IsSafeAsset(asset))
                {
                    EmitDashboardAudit(path, AuditResult.Failed, 404, "InvalidAsset", role);

                    await WriteJsonAsync(ctx, 404, new { error = "not_found" }, ct)
                        .ConfigureAwait(false);
                    return;
                }

                var contentType = GetAssetContentType(asset);
                var bytes = DashboardPageResources.GetBytes(asset);

                EmitDashboardAudit(path, AuditResult.Success, 200, "DashboardAsset", role);
                await WriteAssetAsync(ctx, bytes, contentType, ct).ConfigureAwait(false);
                return;
            }

            if (path is "/" or "/index.html" or "/overview" or "/overview.html")
            {
                if (role == DashboardRole.None)
                {
                    EmitDashboardAudit(
                        path,
                        AuditResult.Denied,
                        401,
                        "Unauthorized",
                        DashboardRole.None
                    );

                    await WriteJsonAsync(ctx, 401, new { error = "unauthorized" }, ct)
                        .ConfigureAwait(false);
                    return;
                }

                EmitDashboardAudit(path, AuditResult.Success, 200, "DashboardHtml", role);
                await WriteHtmlAsync(ctx, "DashboardPage.html", ct).ConfigureAwait(false);
                return;
            }

            if (path is "/investigation" or "/investigation.html")
            {
                if (role == DashboardRole.None)
                {
                    EmitDashboardAudit(
                        path,
                        AuditResult.Denied,
                        401,
                        "Unauthorized",
                        DashboardRole.None
                    );

                    await WriteJsonAsync(ctx, 401, new { error = "unauthorized" }, ct)
                        .ConfigureAwait(false);
                    return;
                }

                EmitDashboardAudit(path, AuditResult.Success, 200, "DashboardHtml", role);
                await WriteHtmlAsync(ctx, "DashboardInvestigationPage.html", ct).ConfigureAwait(false);
                return;
            }

            if (path is "/economy" or "/economy.html")
            {
                if (role == DashboardRole.None)
                {
                    EmitDashboardAudit(
                        path,
                        AuditResult.Denied,
                        401,
                        "Unauthorized",
                        DashboardRole.None
                    );

                    await WriteJsonAsync(ctx, 401, new { error = "unauthorized" }, ct)
                        .ConfigureAwait(false);
                    return;
                }

                EmitDashboardAudit(path, AuditResult.Success, 200, "DashboardHtml", role);
                await WriteHtmlAsync(ctx, "DashboardEconomyPage.html", ct).ConfigureAwait(false);
                return;
            }

            if (path is "/rooms" or "/rooms.html")
            {
                if (role == DashboardRole.None)
                {
                    EmitDashboardAudit(
                        path,
                        AuditResult.Denied,
                        401,
                        "Unauthorized",
                        DashboardRole.None
                    );

                    await WriteJsonAsync(ctx, 401, new { error = "unauthorized" }, ct)
                        .ConfigureAwait(false);
                    return;
                }

                EmitDashboardAudit(path, AuditResult.Success, 200, "DashboardHtml", role);
                await WriteHtmlAsync(ctx, "DashboardRoomsPage.html", ct).ConfigureAwait(false);
                return;
            }

            if (path is "/packets" or "/packets.html")
            {
                if (role == DashboardRole.None)
                {
                    EmitDashboardAudit(
                        path,
                        AuditResult.Denied,
                        401,
                        "Unauthorized",
                        DashboardRole.None
                    );

                    await WriteJsonAsync(ctx, 401, new { error = "unauthorized" }, ct)
                        .ConfigureAwait(false);
                    return;
                }

                EmitDashboardAudit(path, AuditResult.Success, 200, "DashboardHtml", role);
                await WriteHtmlAsync(ctx, "DashboardPacketsPage.html", ct).ConfigureAwait(false);
                return;
            }

            if (path is "/incidents" or "/incidents.html")
            {
                if (role == DashboardRole.None)
                {
                    EmitDashboardAudit(
                        path,
                        AuditResult.Denied,
                        401,
                        "Unauthorized",
                        DashboardRole.None
                    );

                    await WriteJsonAsync(ctx, 401, new { error = "unauthorized" }, ct)
                        .ConfigureAwait(false);
                    return;
                }

                EmitDashboardAudit(path, AuditResult.Success, 200, "DashboardHtml", role);
                await WriteHtmlAsync(ctx, "DashboardIncidentsPage.html", ct).ConfigureAwait(false);
                return;
            }

            if (path is "/audit" or "/audit.html")
            {
                if (role == DashboardRole.None)
                {
                    EmitDashboardAudit(
                        path,
                        AuditResult.Denied,
                        401,
                        "Unauthorized",
                        DashboardRole.None
                    );

                    await WriteJsonAsync(ctx, 401, new { error = "unauthorized" }, ct)
                        .ConfigureAwait(false);
                    return;
                }

                EmitDashboardAudit(path, AuditResult.Success, 200, "DashboardHtml", role);
                await WriteHtmlAsync(ctx, "DashboardAuditPage.html", ct).ConfigureAwait(false);
                return;
            }

            object? payload;

            if (path is "/api/overview")
            {
                if (!CanReadOverview(role))
                {
                    await WriteForbiddenAsync(ctx, path, "UnauthorizedRole", role, ct)
                        .ConfigureAwait(false);
                    return;
                }

                payload = await OverviewAsync(ct).ConfigureAwait(false);
            }
            else if (path is "/api/incidents")
            {
                if (!CanReadOverview(role))
                {
                    await WriteForbiddenAsync(ctx, path, "UnauthorizedRole", role, ct)
                        .ConfigureAwait(false);
                    return;
                }

                payload = await IncidentsAsync(ct).ConfigureAwait(false);
            }
            else if (path is "/api/packet-stats")
            {
                if (!CanReadOverview(role))
                {
                    await WriteForbiddenAsync(ctx, path, "UnauthorizedRole", role, ct)
                        .ConfigureAwait(false);
                    return;
                }

                payload = await PacketStatsAsync(ct).ConfigureAwait(false);
            }
            else if (path is "/api/audit")
            {
                if (!CanReadAudit(role))
                {
                    await WriteForbiddenAsync(ctx, path, "UnauthorizedRole", role, ct)
                        .ConfigureAwait(false);
                    return;
                }

                payload = await AuditAsync(query, ct).ConfigureAwait(false);
            }
            else if (path is "/api/economy")
            {
                if (!CanReadEconomy(role))
                {
                    await WriteForbiddenAsync(ctx, path, "UnauthorizedRole", role, ct)
                        .ConfigureAwait(false);
                    return;
                }

                payload = await EconomyAsync(query, ct).ConfigureAwait(false);
            }
            else if (path is "/api/search")
            {
                if (!CanReadAudit(role))
                {
                    await WriteForbiddenAsync(ctx, path, "UnauthorizedRole", role, ct)
                        .ConfigureAwait(false);
                    return;
                }

                payload = await SearchAsync(query, ct).ConfigureAwait(false);
            }
            else if (path.StartsWith("/api/item/", StringComparison.Ordinal))
            {
                if (!CanReadAudit(role))
                {
                    await WriteForbiddenAsync(ctx, path, "UnauthorizedRole", role, ct)
                        .ConfigureAwait(false);
                    return;
                }

                payload = await ItemAsync(path["/api/item/".Length..], query, ct)
                    .ConfigureAwait(false);
            }
            else if (path.StartsWith("/api/room/", StringComparison.Ordinal))
            {
                if (!CanReadAudit(role))
                {
                    await WriteForbiddenAsync(ctx, path, "UnauthorizedRole", role, ct)
                        .ConfigureAwait(false);
                    return;
                }

                var roomRoute = path["/api/room/".Length..];
                var roomIdText = roomRoute.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                if (string.IsNullOrWhiteSpace(roomIdText) || !int.TryParse(roomIdText, out var roomId))
                {
                    EmitDashboardAudit(path, AuditResult.Failed, 400, "InvalidRoomId", role);

                    await WriteJsonAsync(ctx, 400, new { error = "invalid_room_id" }, ct)
                        .ConfigureAwait(false);
                    return;
                }

                payload = await RoomTimelineAsync(roomId, query, ct).ConfigureAwait(false);
            }
            else
            {
                payload = null;
            }

            if (payload is null)
            {
                EmitDashboardAudit(path, AuditResult.Failed, 404, "NotFound", role);

                await WriteJsonAsync(ctx, 404, new { error = "not_found" }, ct)
                    .ConfigureAwait(false);
                return;
            }

            EmitDashboardAudit(path, AuditResult.Success, 200, "DataResponse", role);

            await WriteJsonAsync(ctx, 200, payload, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(TurboEventIds.DashboardFault, ex, "Turbo dashboard request failed");
            EmitDashboardAudit(
                ctx.Request.Url?.AbsolutePath ?? "/",
                AuditResult.Failed,
                500,
                "InternalError",
                DashboardRole.None
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
        string eventKind,
        DashboardRole role
    )
    {
        _auditSink.Emit(
            new AuditEvent
            {
                Category = AuditCategory.Security,
                Action = "audit.viewed",
                Severity =
                    result == AuditResult.Success ? AuditSeverity.Info : AuditSeverity.Warning,
                Result = result,
                IpHash = null,
                Data = JsonSerializer.Serialize(
                    new
                    {
                        path,
                        status,
                        role = role.ToString().ToLowerInvariant(),
                        kind = eventKind,
                    }
                ),
            }
        );
    }

    private static bool CanReadOverview(DashboardRole role) => role != DashboardRole.None;

    private static bool CanReadAudit(DashboardRole role) =>
        role
            is DashboardRole.Viewer
                or DashboardRole.Moderator
                or DashboardRole.Economy
                or DashboardRole.Admin;

    private static bool CanReadEconomy(DashboardRole role) =>
        role is DashboardRole.Economy or DashboardRole.Admin;

    private async Task<object> PacketStatsAsync(CancellationToken ct)
    {
        var live = await _liveStats.GetSnapshotAsync().ConfigureAwait(false);

        return new
        {
            packetsPerSecond = Math.Round(live.PacketsPerSecond, 2),
            errorsPerMinute = Math.Round(live.ErrorsPerMinute, 2),
            latencyP50Ms = Math.Round(live.LatencyP50Ms, 2),
            latencyP95Ms = Math.Round(live.LatencyP95Ms, 2),
            topOperations = live.TopOperations.Select(o => new
            {
                operation = o.Operation,
                packetsPerMinute = Math.Round(o.PacketsPerMinute, 2),
            }),
            topFailedOperations = live.TopFailedOperations.Select(o => new
            {
                operation = o.Operation,
                packetsPerMinute = Math.Round(o.PacketsPerMinute, 2),
            }),
        };
    }

    private async Task WriteForbiddenAsync(
        HttpListenerContext ctx,
        string path,
        string eventKind,
        DashboardRole role,
        CancellationToken ct
    )
    {
        EmitDashboardAudit(path, AuditResult.Denied, 403, eventKind, role);

        await WriteJsonAsync(ctx, 403, new { error = "forbidden" }, ct).ConfigureAwait(false);
    }

    private async Task<object> OverviewAsync(CancellationToken ct)
    {
        var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            var health = await _infrastructureHealth.GetStatusAsync(ct).ConfigureAwait(false);
            var incidents = await _incidentDetection.DetectAsync(ct).ConfigureAwait(false);
            var live = await _liveStats.GetSnapshotAsync().ConfigureAwait(false);
            var activeRooms = await _grainFactory
                .GetRoomDirectoryGrain()
                .GetActiveRoomsAsync()
                .ConfigureAwait(false);
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
                status = health.Overall,
                health = health,
                uptimeSeconds = (long)(DateTime.UtcNow - _startedAtUtc).TotalSeconds,
                managedMemoryMb = GC.GetTotalMemory(false) / 1024 / 1024,
                activeSessions = _sessionGateway.GetActiveSessionCount(),
                activeRooms = activeRooms.Length,
                incidents = incidents,
                live = new
                {
                    packetsPerSecond = Math.Round(live.PacketsPerSecond, 2),
                    errorsPerMinute = Math.Round(live.ErrorsPerMinute, 2),
                    latencyP50Ms = Math.Round(live.LatencyP50Ms, 2),
                    latencyP95Ms = Math.Round(live.LatencyP95Ms, 2),
                    topAbusers = live.TopAbusers.Select(a => new
                    {
                        playerId = a.PlayerId,
                        packetsPerMinute = a.PacketsPerMinute,
                    }),
                    topRooms = live.TopRooms.Select(r => new
                    {
                        roomId = r.RoomId,
                        packetsPerMinute = r.PacketsPerMinute,
                    }),
                },
                auditLastHourByCategory = byCategory,
                totals = new
                {
                    audit = await db.AuditEvents.CountAsync(ct).ConfigureAwait(false),
                    ledger = await db.EconomyLedger.CountAsync(ct).ConfigureAwait(false),
                    items = await db.ItemEvents.CountAsync(ct).ConfigureAwait(false),
                    performance = await db.PerformanceLogs.CountAsync(ct).ConfigureAwait(false),
                },
            };
        }
        finally
        {
            await db.DisposeAsync().ConfigureAwait(false);
        }
    }

    private Task<IncidentDetectionSnapshot> IncidentsAsync(CancellationToken ct) =>
        _incidentDetection.DetectAsync(ct);

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

                var rows = await q.OrderBy(i => i.OccurredAt)
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
                    var audit = db.AuditEvents.AsNoTracking().Where(a => a.CorrelationId == term);

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

                    var items = db.ItemEvents.AsNoTracking().Where(i => i.CorrelationId == term);

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

                    var ledger = db.EconomyLedger.AsNoTracking().Where(l => l.PlayerId == id);

                    if (since is not null)
                        ledger = ledger.Where(l => l.OccurredAt >= since.Value);

                    if (until is not null)
                        ledger = ledger.Where(l => l.OccurredAt <= until.Value);

                    var itemHistory = db.ItemEvents.AsNoTracking().Where(i => i.ItemId == id);

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
                            .Select(i => new { i.OccurredAt, eventType = i.EventType.ToString() })
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

    private Task<object?> RoomTimelineAsync(
        int roomId,
        NameValueCollection query,
        CancellationToken ct
    ) =>
        QueryAsync<object?>(
            async db =>
            {
                var room = await db.Rooms.AsNoTracking()
                    .Where(r => r.Id == roomId)
                    .Select(r => new
                    {
                        r.Id,
                        r.Name,
                        r.Description,
                        OwnerPlayerId = r.PlayerEntityId,
                        r.UsersNow,
                        r.PlayersMax,
                        LastActive = r.LastActive,
                        ModelName = r.RoomModelEntity.Name,
                    })
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                if (room is null)
                {
                    return null;
                }

                var limit = ParseLimit(query["limit"], 80, 500);
                var page = ParsePage(query["page"]);
                var offset = Math.Max(0, (page - 1) * limit);
                var since = ParseDateTime(query["since"]);
                var until = ParseDateTime(query["until"]);

                var entriesQuery = db.RoomEntryLogs.AsNoTracking().Where(e => e.RoomEntityId == roomId);
                var chatQuery = db.Chatlogs.AsNoTracking().Where(c => c.RoomEntityId == roomId);

                if (since is not null)
                {
                    entriesQuery = entriesQuery.Where(e => e.CreatedAt >= since.Value);
                    chatQuery = chatQuery.Where(c => c.CreatedAt >= since.Value);
                }

                if (until is not null)
                {
                    entriesQuery = entriesQuery.Where(e => e.CreatedAt <= until.Value);
                    chatQuery = chatQuery.Where(c => c.CreatedAt <= until.Value);
                }

                var entryCount = await entriesQuery.CountAsync(ct).ConfigureAwait(false);
                var chatCount = await chatQuery.CountAsync(ct).ConfigureAwait(false);

                var timeline = await entriesQuery
                    .Select(e => new
                    {
                        e.CreatedAt,
                        EventType = "entry",
                        PlayerId = (int?)e.PlayerEntityId,
                        PlayerName = e.PlayerEntity.Name,
                        Message = (string?)null,
                        TargetPlayerId = (int?)null,
                        TargetPlayerName = (string?)null,
                    })
                    .Concat(
                        chatQuery.Select(c => new
                        {
                            c.CreatedAt,
                            EventType = "chat",
                            PlayerId = (int?)c.PlayerEntityId,
                            PlayerName = c.PlayerEntity.Name,
                            Message = (string?)c.Message,
                            TargetPlayerId = (int?)c.TargetPlayerEntityId,
                            TargetPlayerName = c.TargetPlayerEntity != null
                                ? (string?)c.TargetPlayerEntity.Name
                                : null,
                        })
                    )
                    .OrderByDescending(e => e.CreatedAt)
                    .ThenBy(e => e.EventType)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                return new
                {
                    room = new
                    {
                        roomId = room.Id,
                        room.Name,
                        room.Description,
                        room.OwnerPlayerId,
                        room.UsersNow,
                        room.PlayersMax,
                        room.LastActive,
                        room.ModelName,
                    },
                    page,
                    limit,
                    offset,
                    count = timeline.Count,
                    total = entryCount + chatCount,
                    totals = new { entries = entryCount, chats = chatCount },
                    timeline,
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
        ctx.Response.ContentType = JsonContentType;
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes, ct).ConfigureAwait(false);
        ctx.Response.Close();
    }

    private static async Task WriteAssetAsync(
        HttpListenerContext ctx,
        byte[] bytes,
        string contentType,
        CancellationToken ct
    )
    {
        ctx.Response.StatusCode = 200;
        ctx.Response.ContentType = contentType;
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes, ct).ConfigureAwait(false);
        ctx.Response.Close();
    }

    private static async Task WriteHtmlAsync(
        HttpListenerContext ctx,
        string htmlResource,
        CancellationToken ct
    ) => await WriteAssetAsync(ctx, DashboardPageResources.GetBytes(htmlResource), HtmlContentType, ct)
        .ConfigureAwait(false);

    private static string GetAssetContentType(string assetName)
    {
        var extension = System.IO.Path.GetExtension(assetName).ToLowerInvariant();

        return extension switch
        {
            ".css" => CssContentType,
            ".js" => JsContentType,
            ".html" => HtmlContentType,
            _ => "application/octet-stream",
        };
    }

    private static bool IsSafeAsset(string assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName))
            return false;

        if (assetName.Contains('/', StringComparison.Ordinal))
            return false;

        if (assetName.Contains('\\', StringComparison.Ordinal))
            return false;

        if (assetName.Contains("..", StringComparison.Ordinal))
            return false;

        return DashboardPageResources.TryGetResourceName(assetName, out _);
    }

    private DashboardRole ResolveDashboardRole(string? provided)
    {
        if (string.IsNullOrWhiteSpace(provided))
            return DashboardRole.None;

        if (UseLegacyFallbackTokens && TokenMatches(provided, _config.DashboardToken))
            return DashboardRole.Admin;

        if (TokenMatches(provided, _config.DashboardAdminToken))
            return DashboardRole.Admin;

        if (TokenMatches(provided, _config.DashboardEconomyToken))
            return DashboardRole.Economy;

        if (TokenMatches(provided, _config.DashboardModeratorToken))
            return DashboardRole.Moderator;

        if (TokenMatches(provided, _config.DashboardToken))
            return DashboardRole.Viewer;

        return DashboardRole.None;
    }

    private static bool TokenMatches(string provided, string expectedToken)
    {
        if (string.IsNullOrWhiteSpace(expectedToken))
            return false;

        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(expectedToken));
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(provided));
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private enum DashboardRole
    {
        None,
        Viewer,
        Moderator,
        Economy,
        Admin,
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
