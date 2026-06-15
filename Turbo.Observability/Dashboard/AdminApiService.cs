using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Turbo.Observability.Configuration;
using Turbo.Observability.Dashboard.Api;
using Turbo.Observability.Dashboard.Http;
using Turbo.Observability.Dashboard.Infrastructure;
using Turbo.Observability.Dashboard.Operations;
using Turbo.Observability.Dashboard.Security;
using Turbo.Observability.Diagnostics;
using Turbo.Primitives.Observability;

namespace Turbo.Observability.Dashboard;

/// <summary>
/// Native admin dashboard: a small, isolated HTTP front controller over the durable audit tables and
/// the operations layer. It runs on its own <see cref="HttpListener"/> (no ASP.NET dependency, no
/// coupling to the game socket host), binds to localhost by default, and requires a shared token on
/// every request. Reads are GET; controlled admin actions are POST under <c>/api/ops/</c>.
/// </summary>
internal sealed class AdminApiService(
    IOptions<ObservabilityConfig> options,
    DashboardApiService api,
    DashboardOperationsService operations,
    DashboardAccessPolicy accessPolicy,
    DashboardAssetStore assetStore,
    DashboardAuditEmitter dashboardAudit,
    DashboardResponseWriter response,
    ILogger<AdminApiService> logger
) : BackgroundService
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(20);

    private const int MaxOperationBodyBytes = 64 * 1024;

    /// <summary>
    /// The frontend is a client-routed single-page app, so every navigable route returns the same
    /// SPA shell (<c>index.html</c>) and the browser renders the matching view.
    /// </summary>
    private const string ShellHtmlResource = "index.html";

    /// <summary>
    /// Navigable page routes. They all return the SPA shell; the client router renders the view.
    /// Adding a page only needs a new client route plus an entry here so deep links keep resolving.
    /// </summary>
    private static readonly HashSet<string> PageRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",
        "/index.html",
        "/overview",
        "/overview.html",
        "/investigation",
        "/investigation.html",
        "/economy",
        "/economy.html",
        "/rooms",
        "/rooms.html",
        "/packets",
        "/packets.html",
        "/incidents",
        "/incidents.html",
        "/audit",
        "/audit.html",
        "/operations",
        "/operations.html",
    };

    private static readonly JsonSerializerOptions OperationJson = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly ObservabilityConfig _config = options.Value;
    private readonly DashboardApiService _api = api;
    private readonly DashboardOperationsService _operations = operations;
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
        var method = ctx.Request.HttpMethod;

        if (!IsGetOrHead(method) && !IsPost(method))
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
            var path = NormalizePath(ctx.Request.Url?.AbsolutePath);
            var query = ctx.Request.QueryString;

            var token = ctx.Request.Headers["X-Admin-Token"] ?? query["token"];
            var role = _accessPolicy.ResolveRole(token);

            // Mutating admin actions are POST-only and route exclusively to the operations layer.
            if (IsPost(method))
            {
                if (path.StartsWith("/api/ops/", StringComparison.Ordinal))
                    await HandleOperationAsync(ctx, path, role, requestCts.Token)
                        .ConfigureAwait(false);
                else
                    await _response
                        .WriteJsonAsync(
                            ctx,
                            405,
                            new { error = "method_not_allowed" },
                            requestCts.Token
                        )
                        .ConfigureAwait(false);
                return;
            }

            // Bundled SPA static assets (hashed js/css). Not sensitive, served regardless of role.
            if (path.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase))
            {
                await ServeAssetAsync(ctx, path, role, requestCts.Token).ConfigureAwait(false);
                return;
            }

            // SPA shell: every navigable page returns index.html; the client router renders it.
            if (PageRoutes.Contains(path))
            {
                await ServeShellAsync(ctx, path, role, requestCts.Token).ConfigureAwait(false);
                return;
            }

            // Read-only JSON API.
            if (path.StartsWith("/api/", StringComparison.Ordinal))
            {
                var outcome = await DispatchApiAsync(path, role, query, requestCts.Token)
                    .ConfigureAwait(false);

                await WriteApiOutcomeAsync(ctx, path, role, outcome, requestCts.Token)
                    .ConfigureAwait(false);
                return;
            }

            _dashboardAudit.Emit(path, AuditResult.Failed, 404, "NotFound", role);
            await _response
                .WriteJsonAsync(ctx, 404, new { error = "not_found" }, requestCts.Token)
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

    private async Task ServeAssetAsync(
        HttpListenerContext ctx,
        string path,
        DashboardRole role,
        CancellationToken ct
    )
    {
        var asset = path["/assets/".Length..];

        if (!_assetStore.TryGetAsset(asset, out var bytes, out var contentType))
        {
            _dashboardAudit.Emit(path, AuditResult.Failed, 404, "InvalidAsset", role);

            await _response
                .WriteJsonAsync(ctx, 404, new { error = "not_found" }, ct)
                .ConfigureAwait(false);
            return;
        }

        // Static SPA assets are not sensitive and load on every page view; auditing them only adds
        // noise to the very tables the dashboard exists to investigate, so successful serves are not
        // audited. Data reads and admin operations remain audited.
        await _response.WriteBytesAsync(ctx, 200, bytes, contentType, ct).ConfigureAwait(false);
    }

    private async Task ServeShellAsync(
        HttpListenerContext ctx,
        string path,
        DashboardRole role,
        CancellationToken ct
    )
    {
        if (role == DashboardRole.None)
        {
            _dashboardAudit.Emit(path, AuditResult.Denied, 401, "Unauthorized", DashboardRole.None);

            await _response
                .WriteJsonAsync(ctx, 401, new { error = "unauthorized" }, ct)
                .ConfigureAwait(false);
            return;
        }

        // The shell is the same static document for every page; a successful serve is not an action
        // worth a durable audit row (see ServeAssetAsync). Denials above are still audited.
        await _response
            .WriteBytesAsync(
                ctx,
                200,
                _assetStore.GetHtmlBytes(ShellHtmlResource),
                _assetStore.HtmlContentType,
                ct
            )
            .ConfigureAwait(false);
    }

    private async Task<ApiOutcome> DispatchApiAsync(
        string path,
        DashboardRole role,
        NameValueCollection query,
        CancellationToken ct
    )
    {
        switch (path)
        {
            case "/api/overview":
                if (!_accessPolicy.CanReadOverview(role))
                    return ApiOutcome.Forbidden;

                return ApiOutcome.Ok(
                    await _api.OverviewAsync(_startedAtUtc, ct).ConfigureAwait(false)
                );

            case "/api/infrastructure":
                if (!_accessPolicy.CanReadOverview(role))
                    return ApiOutcome.Forbidden;

                return ApiOutcome.Ok(await _api.InfrastructureAsync(ct).ConfigureAwait(false));

            case "/api/incidents":
                if (!_accessPolicy.CanReadOverview(role))
                    return ApiOutcome.Forbidden;

                return ApiOutcome.Ok(await _api.IncidentsAsync(ct).ConfigureAwait(false));

            case "/api/packet-stats":
                if (!_accessPolicy.CanReadOverview(role))
                    return ApiOutcome.Forbidden;

                return ApiOutcome.Ok(await _api.PacketStatsAsync(ct).ConfigureAwait(false));

            case "/api/audit":
                if (!_accessPolicy.CanReadAudit(role))
                    return ApiOutcome.Forbidden;

                return ApiOutcome.Ok(await _api.AuditAsync(query, ct).ConfigureAwait(false));

            case "/api/economy":
                if (!_accessPolicy.CanReadEconomy(role))
                    return ApiOutcome.Forbidden;

                return ApiOutcome.Ok(await _api.EconomyAsync(query, ct).ConfigureAwait(false));

            case "/api/search":
                if (!_accessPolicy.CanReadAudit(role))
                    return ApiOutcome.Forbidden;

                return ApiOutcome.Ok(await _api.SearchAsync(query, ct).ConfigureAwait(false));

            case "/api/players":
                if (!_accessPolicy.CanReadOverview(role))
                    return ApiOutcome.Forbidden;

                return ApiOutcome.Ok(await _api.PlayersAsync(query, ct).ConfigureAwait(false));

            case "/api/furniture":
                if (!_accessPolicy.CanReadOverview(role))
                    return ApiOutcome.Forbidden;

                return ApiOutcome.Ok(
                    await _api.FurnitureDefinitionsAsync(query, ct).ConfigureAwait(false)
                );
        }

        if (path.StartsWith("/api/item/", StringComparison.Ordinal))
        {
            if (!_accessPolicy.CanReadAudit(role))
                return ApiOutcome.Forbidden;

            return ApiOutcome.Ok(
                await _api.ItemAsync(path["/api/item/".Length..], query, ct).ConfigureAwait(false)
            );
        }

        if (path.StartsWith("/api/room/", StringComparison.Ordinal))
        {
            if (!_accessPolicy.CanReadAudit(role))
                return ApiOutcome.Forbidden;

            var roomRoute = path["/api/room/".Length..];
            var roomIdText = roomRoute
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(roomIdText) || !int.TryParse(roomIdText, out var roomId))
                return ApiOutcome.BadRequest("InvalidRoomId", "invalid_room_id");

            return ApiOutcome.Ok(
                await _api.RoomTimelineAsync(roomId, query, ct).ConfigureAwait(false)
            );
        }

        return ApiOutcome.NotFound;
    }

    private async Task WriteApiOutcomeAsync(
        HttpListenerContext ctx,
        string path,
        DashboardRole role,
        ApiOutcome outcome,
        CancellationToken ct
    )
    {
        // A handler that ran but produced no payload (unknown id, missing room/item) is a 404.
        if (outcome.Status == 200 && outcome.Payload is null)
            outcome = ApiOutcome.NotFound;

        var auditResult = outcome.Status switch
        {
            200 => AuditResult.Success,
            403 => AuditResult.Denied,
            _ => AuditResult.Failed,
        };

        _dashboardAudit.Emit(path, auditResult, outcome.Status, outcome.AuditKind, role);

        var body = outcome.Status == 200 ? outcome.Payload! : new { error = outcome.ErrorCode };
        await _response.WriteJsonAsync(ctx, outcome.Status, body, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Front-controller for controlled admin actions: Admin role only, mandatory request body, then
    /// dispatch to the operations service which performs the action through grains and audits it.
    /// </summary>
    private async Task HandleOperationAsync(
        HttpListenerContext ctx,
        string path,
        DashboardRole role,
        CancellationToken ct
    )
    {
        if (role != DashboardRole.Admin)
        {
            _dashboardAudit.Emit(path, AuditResult.Denied, 403, "OpsUnauthorized", role);

            await _response
                .WriteJsonAsync(ctx, 403, new { error = "forbidden" }, ct)
                .ConfigureAwait(false);
            return;
        }

        var body = await ReadBodyAsync(ctx.Request, ct).ConfigureAwait(false);

        if (body is null)
        {
            _dashboardAudit.Emit(path, AuditResult.Failed, 413, "OpsBodyTooLarge", role);

            await _response
                .WriteJsonAsync(ctx, 413, new { error = "payload_too_large" }, ct)
                .ConfigureAwait(false);
            return;
        }

        var (status, payload, errorCode) = await DispatchOperationAsync(path, role, body, ct)
            .ConfigureAwait(false);

        if (status == 200)
        {
            // The operation itself is audited (with correlation id) by DashboardOperationsService.
            await _response.WriteJsonAsync(ctx, 200, payload!, ct).ConfigureAwait(false);
            return;
        }

        _dashboardAudit.Emit(
            path,
            AuditResult.Failed,
            status,
            status == 404 ? "OpsUnknown" : "OpsInvalid",
            role
        );

        await _response
            .WriteJsonAsync(ctx, status, new { error = errorCode }, ct)
            .ConfigureAwait(false);
    }

    private async Task<(int Status, object? Payload, string ErrorCode)> DispatchOperationAsync(
        string path,
        DashboardRole role,
        string body,
        CancellationToken ct
    )
    {
        switch (path)
        {
            case "/api/ops/currency/credits":
                if (
                    !TryParseBody<GiveCreditsRequest>(body, out var credits)
                    || credits.PlayerId <= 0
                    || credits.Amount <= 0
                    || !HasReason(credits.Reason)
                )
                    return (400, null, "invalid_request");

                return (
                    200,
                    await _operations.GiveCreditsAsync(credits, role, ct).ConfigureAwait(false),
                    string.Empty
                );

            case "/api/ops/currency/activity-points":
                if (
                    !TryParseBody<GiveActivityPointsRequest>(body, out var activityPoints)
                    || activityPoints.PlayerId <= 0
                    || activityPoints.Type < 0
                    || activityPoints.Amount <= 0
                    || !HasReason(activityPoints.Reason)
                )
                    return (400, null, "invalid_request");

                return (
                    200,
                    await _operations
                        .GiveActivityPointsAsync(activityPoints, role, ct)
                        .ConfigureAwait(false),
                    string.Empty
                );

            case "/api/ops/item/grant":
                if (
                    !TryParseBody<GiveFurnitureRequest>(body, out var furniture)
                    || furniture.PlayerId <= 0
                    || furniture.DefinitionId <= 0
                    || !HasReason(furniture.Reason)
                )
                    return (400, null, "invalid_request");

                return (
                    200,
                    await _operations.GiveFurnitureAsync(furniture, role, ct).ConfigureAwait(false),
                    string.Empty
                );

            case "/api/ops/player/kick":
                if (
                    !TryParseBody<KickPlayerRequest>(body, out var kick)
                    || kick.PlayerId <= 0
                    || !HasReason(kick.Reason)
                )
                    return (400, null, "invalid_request");

                return (
                    200,
                    await _operations.KickPlayerAsync(kick, role, ct).ConfigureAwait(false),
                    string.Empty
                );
        }

        return (404, null, "not_found");
    }

    private static async Task<string?> ReadBodyAsync(
        HttpListenerRequest request,
        CancellationToken ct
    )
    {
        if (request.ContentLength64 > MaxOperationBodyBytes)
            return null;

        using var reader = new StreamReader(
            request.InputStream,
            request.ContentEncoding ?? Encoding.UTF8
        );

        return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
    }

    private static bool TryParseBody<T>(string body, out T value)
        where T : class
    {
        try
        {
            value = JsonSerializer.Deserialize<T>(body, OperationJson)!;
            return value is not null;
        }
        catch (JsonException)
        {
            value = null!;
            return false;
        }
    }

    private static bool HasReason(string? reason) =>
        !string.IsNullOrWhiteSpace(reason) && reason.Trim().Length >= 3;

    private static string NormalizePath(string? absolutePath)
    {
        var path = absolutePath?.TrimEnd('/') ?? "/";

        return string.IsNullOrEmpty(path) ? "/" : path;
    }

    private static bool IsGetOrHead(string? method) =>
        string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
        || string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase);

    private static bool IsPost(string? method) =>
        string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Result of dispatching a read JSON API route: the HTTP status, the audit event kind, and either
    /// a payload (200) or a machine-readable error code. Centralizing this lets the request loop emit
    /// the audit event and write the response once instead of repeating it per route.
    /// </summary>
    private readonly record struct ApiOutcome(
        int Status,
        string AuditKind,
        object? Payload,
        string ErrorCode
    )
    {
        public static ApiOutcome Ok(object? payload) =>
            new(200, "DataResponse", payload, string.Empty);

        public static ApiOutcome Forbidden { get; } =
            new(403, "UnauthorizedRole", null, "forbidden");

        public static ApiOutcome NotFound { get; } = new(404, "NotFound", null, "not_found");

        public static ApiOutcome BadRequest(string auditKind, string errorCode) =>
            new(400, auditKind, null, errorCode);
    }
}
