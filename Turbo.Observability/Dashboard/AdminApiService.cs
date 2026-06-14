using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Turbo.Observability.Configuration;
using Turbo.Observability.Dashboard.Api;
using Turbo.Observability.Dashboard.Http;
using Turbo.Observability.Dashboard.Infrastructure;
using Turbo.Observability.Dashboard.Security;
using Turbo.Observability.Diagnostics;
using Turbo.Primitives.Observability;

namespace Turbo.Observability.Dashboard;

/// <summary>
/// Native admin dashboard: a small, isolated, read-only HTTP API over the durable audit tables. It
/// runs on its own <see cref="HttpListener"/> (no ASP.NET dependency, no coupling to the game socket
/// host), binds to localhost by default, and requires a shared token on every request.
/// </summary>
internal sealed class AdminApiService(
    IOptions<ObservabilityConfig> options,
    DashboardApiService api,
    DashboardAccessPolicy accessPolicy,
    DashboardAssetStore assetStore,
    DashboardAuditEmitter dashboardAudit,
    DashboardResponseWriter response,
    ILogger<AdminApiService> logger
) : BackgroundService
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(20);

    private readonly ObservabilityConfig _config = options.Value;
    private readonly DashboardApiService _api = api;
    private readonly DashboardAccessPolicy _accessPolicy = accessPolicy;
    private readonly DashboardAssetStore _assetStore = assetStore;
    private readonly DashboardAuditEmitter _dashboardAudit = dashboardAudit;
    private readonly DashboardResponseWriter _response = response;
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
        listener.IgnoreWriteExceptions = true;

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
        if (!IsGetOrHead(ctx.Request.HttpMethod))
        {
            await _response
                .WriteJsonAsync(ctx, 405, new { error = "method_not_allowed" }, ct)
                .ConfigureAwait(false);
            return;
        }

        using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        requestCts.CancelAfter(RequestTimeout);

        try
        {
            var path = ctx.Request.Url?.AbsolutePath.TrimEnd('/') ?? "/";
            if (string.IsNullOrEmpty(path))
                path = "/";

            if (path.Length > 1 && !path.StartsWith('/'))
                path = $"/{path}";

            var query = ctx.Request.QueryString;

            var token = ctx.Request.Headers["X-Admin-Token"] ?? query["token"];
            var role = _accessPolicy.ResolveRole(token);

            if (path.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase))
            {
                var asset = path["/assets/".Length..];

                if (!_assetStore.TryGetAsset(asset, out var bytes, out var contentType))
                {
                    _dashboardAudit.Emit(path, AuditResult.Failed, 404, "InvalidAsset", role);

                    await _response
                        .WriteJsonAsync(ctx, 404, new { error = "not_found" }, requestCts.Token)
                        .ConfigureAwait(false);
                    return;
                }

                _dashboardAudit.Emit(path, AuditResult.Success, 200, "DashboardAsset", role);
                await _response
                    .WriteBytesAsync(ctx, 200, bytes, contentType, requestCts.Token)
                    .ConfigureAwait(false);
                return;
            }

            if (path is "/" or "/index.html" or "/overview" or "/overview.html")
            {
                if (role == DashboardRole.None)
                {
                    _dashboardAudit.Emit(
                        path,
                        AuditResult.Denied,
                        401,
                        "Unauthorized",
                        DashboardRole.None
                    );

                    await _response
                        .WriteJsonAsync(ctx, 401, new { error = "unauthorized" }, requestCts.Token)
                        .ConfigureAwait(false);
                    return;
                }

                _dashboardAudit.Emit(path, AuditResult.Success, 200, "DashboardHtml", role);
                await WriteHtmlAsync(ctx, "index.html", requestCts.Token).ConfigureAwait(false);
                return;
            }

            if (path is "/investigation" or "/investigation.html")
            {
                if (role == DashboardRole.None)
                {
                    _dashboardAudit.Emit(
                        path,
                        AuditResult.Denied,
                        401,
                        "Unauthorized",
                        DashboardRole.None
                    );

                    await _response
                        .WriteJsonAsync(ctx, 401, new { error = "unauthorized" }, requestCts.Token)
                        .ConfigureAwait(false);
                    return;
                }

                _dashboardAudit.Emit(path, AuditResult.Success, 200, "DashboardHtml", role);
                await WriteHtmlAsync(ctx, "index.html", requestCts.Token).ConfigureAwait(false);
                return;
            }

            if (path is "/economy" or "/economy.html")
            {
                if (role == DashboardRole.None)
                {
                    _dashboardAudit.Emit(
                        path,
                        AuditResult.Denied,
                        401,
                        "Unauthorized",
                        DashboardRole.None
                    );

                    await _response
                        .WriteJsonAsync(ctx, 401, new { error = "unauthorized" }, requestCts.Token)
                        .ConfigureAwait(false);
                    return;
                }

                _dashboardAudit.Emit(path, AuditResult.Success, 200, "DashboardHtml", role);
                await WriteHtmlAsync(ctx, "index.html", requestCts.Token).ConfigureAwait(false);
                return;
            }

            if (path is "/rooms" or "/rooms.html")
            {
                if (role == DashboardRole.None)
                {
                    _dashboardAudit.Emit(
                        path,
                        AuditResult.Denied,
                        401,
                        "Unauthorized",
                        DashboardRole.None
                    );

                    await _response
                        .WriteJsonAsync(ctx, 401, new { error = "unauthorized" }, requestCts.Token)
                        .ConfigureAwait(false);
                    return;
                }

                _dashboardAudit.Emit(path, AuditResult.Success, 200, "DashboardHtml", role);
                await WriteHtmlAsync(ctx, "index.html", requestCts.Token).ConfigureAwait(false);
                return;
            }

            if (path is "/packets" or "/packets.html")
            {
                if (role == DashboardRole.None)
                {
                    _dashboardAudit.Emit(
                        path,
                        AuditResult.Denied,
                        401,
                        "Unauthorized",
                        DashboardRole.None
                    );

                    await _response
                        .WriteJsonAsync(ctx, 401, new { error = "unauthorized" }, requestCts.Token)
                        .ConfigureAwait(false);
                    return;
                }

                _dashboardAudit.Emit(path, AuditResult.Success, 200, "DashboardHtml", role);
                await WriteHtmlAsync(ctx, "index.html", requestCts.Token).ConfigureAwait(false);
                return;
            }

            if (path is "/incidents" or "/incidents.html")
            {
                if (role == DashboardRole.None)
                {
                    _dashboardAudit.Emit(
                        path,
                        AuditResult.Denied,
                        401,
                        "Unauthorized",
                        DashboardRole.None
                    );

                    await _response
                        .WriteJsonAsync(ctx, 401, new { error = "unauthorized" }, requestCts.Token)
                        .ConfigureAwait(false);
                    return;
                }

                _dashboardAudit.Emit(path, AuditResult.Success, 200, "DashboardHtml", role);
                await WriteHtmlAsync(ctx, "index.html", requestCts.Token).ConfigureAwait(false);
                return;
            }

            if (path is "/audit" or "/audit.html")
            {
                if (role == DashboardRole.None)
                {
                    _dashboardAudit.Emit(
                        path,
                        AuditResult.Denied,
                        401,
                        "Unauthorized",
                        DashboardRole.None
                    );

                    await _response
                        .WriteJsonAsync(ctx, 401, new { error = "unauthorized" }, requestCts.Token)
                        .ConfigureAwait(false);
                    return;
                }

                _dashboardAudit.Emit(path, AuditResult.Success, 200, "DashboardHtml", role);
                await WriteHtmlAsync(ctx, "index.html", requestCts.Token).ConfigureAwait(false);
                return;
            }

            object? payload;

            if (path is "/api/overview")
            {
                if (!_accessPolicy.CanReadOverview(role))
                {
                    await WriteForbiddenAsync(ctx, path, "UnauthorizedRole", role, requestCts.Token)
                        .ConfigureAwait(false);
                    return;
                }

                payload = await _api.OverviewAsync(_startedAtUtc, requestCts.Token)
                    .ConfigureAwait(false);
            }
            else if (path is "/api/infrastructure")
            {
                if (!_accessPolicy.CanReadOverview(role))
                {
                    await WriteForbiddenAsync(ctx, path, "UnauthorizedRole", role, requestCts.Token)
                        .ConfigureAwait(false);
                    return;
                }

                payload = await _api.InfrastructureAsync(requestCts.Token).ConfigureAwait(false);
            }
            else if (path is "/api/incidents")
            {
                if (!_accessPolicy.CanReadOverview(role))
                {
                    await WriteForbiddenAsync(ctx, path, "UnauthorizedRole", role, requestCts.Token)
                        .ConfigureAwait(false);
                    return;
                }

                payload = await _api.IncidentsAsync(requestCts.Token).ConfigureAwait(false);
            }
            else if (path is "/api/packet-stats")
            {
                if (!_accessPolicy.CanReadOverview(role))
                {
                    await WriteForbiddenAsync(ctx, path, "UnauthorizedRole", role, requestCts.Token)
                        .ConfigureAwait(false);
                    return;
                }

                payload = await _api.PacketStatsAsync(requestCts.Token).ConfigureAwait(false);
            }
            else if (path is "/api/audit")
            {
                if (!_accessPolicy.CanReadAudit(role))
                {
                    await WriteForbiddenAsync(ctx, path, "UnauthorizedRole", role, requestCts.Token)
                        .ConfigureAwait(false);
                    return;
                }

                payload = await _api.AuditAsync(query, requestCts.Token).ConfigureAwait(false);
            }
            else if (path is "/api/economy")
            {
                if (!_accessPolicy.CanReadEconomy(role))
                {
                    await WriteForbiddenAsync(ctx, path, "UnauthorizedRole", role, requestCts.Token)
                        .ConfigureAwait(false);
                    return;
                }

                payload = await _api.EconomyAsync(query, requestCts.Token).ConfigureAwait(false);
            }
            else if (path is "/api/search")
            {
                if (!_accessPolicy.CanReadAudit(role))
                {
                    await WriteForbiddenAsync(ctx, path, "UnauthorizedRole", role, requestCts.Token)
                        .ConfigureAwait(false);
                    return;
                }

                payload = await _api.SearchAsync(query, requestCts.Token).ConfigureAwait(false);
            }
            else if (path.StartsWith("/api/item/", StringComparison.Ordinal))
            {
                if (!_accessPolicy.CanReadAudit(role))
                {
                    await WriteForbiddenAsync(ctx, path, "UnauthorizedRole", role, requestCts.Token)
                        .ConfigureAwait(false);
                    return;
                }

                payload = await _api.ItemAsync(path["/api/item/".Length..], query, requestCts.Token)
                    .ConfigureAwait(false);
            }
            else if (path.StartsWith("/api/room/", StringComparison.Ordinal))
            {
                if (!_accessPolicy.CanReadAudit(role))
                {
                    await WriteForbiddenAsync(ctx, path, "UnauthorizedRole", role, requestCts.Token)
                        .ConfigureAwait(false);
                    return;
                }

                var roomRoute = path["/api/room/".Length..];
                var roomIdText = roomRoute
                    .Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();

                if (
                    string.IsNullOrWhiteSpace(roomIdText)
                    || !int.TryParse(roomIdText, out var roomId)
                )
                {
                    _dashboardAudit.Emit(path, AuditResult.Failed, 400, "InvalidRoomId", role);

                    await _response
                        .WriteJsonAsync(
                            ctx,
                            400,
                            new { error = "invalid_room_id" },
                            requestCts.Token
                        )
                        .ConfigureAwait(false);
                    return;
                }

                payload = await _api.RoomTimelineAsync(roomId, query, requestCts.Token)
                    .ConfigureAwait(false);
            }
            else
            {
                payload = null;
            }

            if (payload is null)
            {
                _dashboardAudit.Emit(path, AuditResult.Failed, 404, "NotFound", role);

                await _response
                    .WriteJsonAsync(ctx, 404, new { error = "not_found" }, requestCts.Token)
                    .ConfigureAwait(false);
                return;
            }

            _dashboardAudit.Emit(path, AuditResult.Success, 200, "DataResponse", role);

            await _response
                .WriteJsonAsync(ctx, 200, payload, requestCts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _dashboardAudit.Emit(
                ctx.Request.Url?.AbsolutePath ?? "/",
                AuditResult.Failed,
                408,
                "RequestTimeout",
                DashboardRole.None
            );

            try
            {
                await _response
                    .WriteJsonAsync(ctx, 408, new { error = "timeout" }, ct)
                    .ConfigureAwait(false);
            }
            catch
            {
                // best effort
            }
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested)
                return;

            _logger.LogError(TurboEventIds.DashboardFault, ex, "Turbo dashboard request failed");
            _dashboardAudit.Emit(
                ctx.Request.Url?.AbsolutePath ?? "/",
                AuditResult.Failed,
                500,
                "InternalError",
                DashboardRole.None
            );

            try
            {
                await _response
                    .WriteJsonAsync(ctx, 500, new { error = "internal" }, requestCts.Token)
                    .ConfigureAwait(false);
            }
            catch
            {
                // best effort
            }
        }
    }

    private async Task WriteHtmlAsync(
        HttpListenerContext ctx,
        string htmlResource,
        CancellationToken ct
    ) =>
        await _response
            .WriteBytesAsync(
                ctx,
                200,
                _assetStore.GetHtmlBytes(htmlResource),
                _assetStore.HtmlContentType,
                ct
            )
            .ConfigureAwait(false);

    private async Task WriteForbiddenAsync(
        HttpListenerContext ctx,
        string path,
        string eventKind,
        DashboardRole role,
        CancellationToken ct
    )
    {
        _dashboardAudit.Emit(path, AuditResult.Denied, 403, eventKind, role);

        await _response
            .WriteJsonAsync(ctx, 403, new { error = "forbidden" }, ct)
            .ConfigureAwait(false);
    }

    private static bool IsHeadRequest(string? method) =>
        string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase);

    private static bool IsGetOrHead(string? method) =>
        string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) || IsHeadRequest(method);
}
