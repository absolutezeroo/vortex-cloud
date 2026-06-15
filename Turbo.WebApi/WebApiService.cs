using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Turbo.WebApi.Configuration;
using Turbo.WebApi.Http;
using Turbo.WebApi.Services;
using Turbo.WebApi.Session;

namespace Turbo.WebApi;

/// <summary>
/// Native HTTP server that exposes the client-facing web API used by the Flash onboarding flow:
/// login, registration, avatar list, SSO token generation. One HttpListener, no ASP.NET.
/// </summary>
internal sealed class WebApiService(
    IOptions<WebApiConfig> options,
    IWebApiAuthService auth,
    IWebApiPlayerService players,
    WebApiSessionStore sessions,
    WebApiResponseWriter response,
    ILogger<WebApiService> logger
) : BackgroundService
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(20);
    private const int MaxBodyBytes = 64 * 1024;

    private static readonly JsonSerializerOptions BodyJson = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly WebApiConfig _config = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Enabled)
            return;

        using var listener = new HttpListener();
        var prefix = $"http://{_config.Host}:{_config.Port}/";
        listener.Prefixes.Add(prefix);
        listener.IgnoreWriteExceptions = true;

        try
        {
            listener.Start();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start WebApi on {Prefix}", prefix);
            return;
        }

        using var stopRegistration = stoppingToken.Register(listener.Stop);
        logger.LogInformation("Vortex WebApi listening on {Prefix}", prefix);

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
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(RequestTimeout);

        var method = ctx.Request.HttpMethod?.ToUpperInvariant() ?? "GET";
        var path = ctx.Request.Url?.AbsolutePath?.TrimEnd('/') ?? "/";
        if (string.IsNullOrEmpty(path))
            path = "/";

        // Pre-flight CORS
        if (method == "OPTIONS")
        {
            await response.WriteJsonAsync(ctx, 204, new { }, null, cts.Token).ConfigureAwait(false);
            return;
        }

        try
        {
            await DispatchAsync(ctx, method, path, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            try
            {
                await response.WriteErrorAsync(ctx, 408, "timeout", ct).ConfigureAwait(false);
            }
            catch { }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WebApi request error: {Method} {Path}", method, path);

            try
            {
                await response.WriteErrorAsync(ctx, 500, "internal", cts.Token).ConfigureAwait(false);
            }
            catch { }
        }
    }

    private async Task DispatchAsync(
        HttpListenerContext ctx,
        string method,
        string path,
        CancellationToken ct
    )
    {
        var ip = ctx.Request.RemoteEndPoint?.Address?.ToString() ?? "127.0.0.1";
        var sessionId = ReadSessionCookie(ctx.Request);
        var accountId = sessions.GetAccountId(sessionId);

        switch (path)
        {
            // ── Public: server hello ──────────────────────────────────────────
            case "/api/public/info/hello" when method == "GET":
                await response
                    .WriteJsonAsync(ctx, 200, new { status = "ok" }, null, ct)
                    .ConfigureAwait(false);
                return;

            // ── Authentication: login ─────────────────────────────────────────
            case "/api/public/authentication/login" when method == "POST":
                await HandleLoginAsync(ctx, ip, ct).ConfigureAwait(false);
                return;

            // ── Authentication: logout ────────────────────────────────────────
            case "/api/public/authentication/logout" when method == "POST":
                if (sessionId is not null)
                    sessions.RemoveSession(sessionId);
                await response
                    .WriteJsonAsync(ctx, 200, new { }, null, ct)
                    .ConfigureAwait(false);
                return;

            // ── Registration: new account ─────────────────────────────────────
            case "/api/public/registration/new" when method == "POST":
                await HandleRegisterAsync(ctx, ct).ConfigureAwait(false);
                return;

            // ── Avatars: list ─────────────────────────────────────────────────
            case "/api/user/avatars" when method == "GET":
                await HandleGetAvatarsAsync(ctx, accountId, ct).ConfigureAwait(false);
                return;

            // ── Avatars: create ───────────────────────────────────────────────
            case "/api/user/avatars" when method == "POST":
                await HandleCreateAvatarAsync(ctx, accountId, ct).ConfigureAwait(false);
                return;

            // ── Avatars: select (returns SSO for the chosen avatar) ───────────
            case "/api/user/avatars/select" when method == "POST":
                await HandleSelectAvatarAsync(ctx, accountId, sessionId, ct).ConfigureAwait(false);
                return;

            // ── SSO token ─────────────────────────────────────────────────────
            case "/api/ssotoken" when method == "GET":
                await HandleSsoTokenAsync(ctx, accountId, sessionId, ip, ct).ConfigureAwait(false);
                return;

            // ── Name check ────────────────────────────────────────────────────
            case "/api/newuser/name/check" when method == "POST":
                await HandleNameCheckAsync(ctx, ct).ConfigureAwait(false);
                return;

            // ── Name select ───────────────────────────────────────────────────
            case "/api/newuser/name/select" when method == "POST":
                await HandleNameSelectAsync(ctx, accountId, ct).ConfigureAwait(false);
                return;

            // ── Save figure ───────────────────────────────────────────────────
            case "/api/user/look/save" when method == "POST":
                await HandleSaveFigureAsync(ctx, accountId, ct).ConfigureAwait(false);
                return;

            // ── Room select (no-op for now) ───────────────────────────────────
            case "/api/newuser/room/select" when method == "POST":
                await response
                    .WriteJsonAsync(ctx, 200, new { }, null, ct)
                    .ConfigureAwait(false);
                return;
        }

        await response.WriteErrorAsync(ctx, 404, "not_found", ct).ConfigureAwait(false);
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task HandleLoginAsync(HttpListenerContext ctx, string ip, CancellationToken ct)
    {
        var body = await ReadBodyAsync<LoginRequest>(ctx.Request, ct).ConfigureAwait(false);

        if (body is null || string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
        {
            await response
                .WriteErrorAsync(ctx, 400, "pocket.auth.missing_credentials", ct)
                .ConfigureAwait(false);
            return;
        }

        var (success, sessionId, error) = await auth
            .LoginAsync(body.Email, body.Password, ct)
            .ConfigureAwait(false);

        if (!success)
        {
            await response
                .WriteJsonAsync(ctx, 401, new { error }, null, ct)
                .ConfigureAwait(false);
            return;
        }

        await response.WriteJsonAsync(ctx, 200, new { }, sessionId, ct).ConfigureAwait(false);
    }

    private async Task HandleRegisterAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        var body = await ReadBodyAsync<RegisterRequest>(ctx.Request, ct).ConfigureAwait(false);

        if (body is null || string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Password))
        {
            await response
                .WriteErrorAsync(ctx, 400, "pocket.auth.missing_credentials", ct)
                .ConfigureAwait(false);
            return;
        }

        var (success, accountId, error) = await auth
            .RegisterAsync(body.Email, body.Password, ct)
            .ConfigureAwait(false);

        if (!success)
        {
            await response
                .WriteJsonAsync(ctx, 409, new { error }, null, ct)
                .ConfigureAwait(false);
            return;
        }

        // Auto-login after registration
        var (loginOk, sessionId, _) = await auth
            .LoginAsync(body.Email, body.Password, ct)
            .ConfigureAwait(false);

        await response
            .WriteJsonAsync(ctx, 200, new { id = accountId }, loginOk ? sessionId : null, ct)
            .ConfigureAwait(false);
    }

    private async Task HandleGetAvatarsAsync(
        HttpListenerContext ctx,
        int? accountId,
        CancellationToken ct
    )
    {
        if (accountId is null)
        {
            await response.WriteErrorAsync(ctx, 401, "unauthorized", ct).ConfigureAwait(false);
            return;
        }

        var avatars = await players
            .GetAvatarsForAccountAsync(accountId.Value, ct)
            .ConfigureAwait(false);

        await response.WriteJsonAsync(ctx, 200, avatars, null, ct).ConfigureAwait(false);
    }

    private async Task HandleCreateAvatarAsync(
        HttpListenerContext ctx,
        int? accountId,
        CancellationToken ct
    )
    {
        if (accountId is null)
        {
            await response.WriteErrorAsync(ctx, 401, "unauthorized", ct).ConfigureAwait(false);
            return;
        }

        var body = await ReadBodyAsync<CreateAvatarRequest>(ctx.Request, ct).ConfigureAwait(false);

        if (body is null || string.IsNullOrWhiteSpace(body.Name))
        {
            await response.WriteErrorAsync(ctx, 400, "invalid_request", ct).ConfigureAwait(false);
            return;
        }

        var (success, _, error) = await players
            .CreateAvatarAsync(
                accountId.Value,
                body.Name,
                body.Figure ?? string.Empty,
                body.Gender ?? "M",
                ct
            )
            .ConfigureAwait(false);

        if (!success)
        {
            await response
                .WriteJsonAsync(ctx, 409, new { error }, null, ct)
                .ConfigureAwait(false);
            return;
        }

        // Return the updated avatar list so the client handler can process it identically
        // to a GET /api/user/avatars response (auto-select if single, show list otherwise).
        var avatars = await players
            .GetAvatarsForAccountAsync(accountId.Value, ct)
            .ConfigureAwait(false);

        await response.WriteJsonAsync(ctx, 200, avatars, null, ct).ConfigureAwait(false);
    }

    private async Task HandleSelectAvatarAsync(
        HttpListenerContext ctx,
        int? accountId,
        string? sessionId,
        CancellationToken ct
    )
    {
        if (accountId is null)
        {
            await response.WriteErrorAsync(ctx, 401, "unauthorized", ct).ConfigureAwait(false);
            return;
        }

        var body = await ReadBodyAsync<SelectAvatarRequest>(ctx.Request, ct).ConfigureAwait(false);

        if (body is null || string.IsNullOrWhiteSpace(body.UniqueId))
        {
            await response.WriteErrorAsync(ctx, 400, "invalid_request", ct).ConfigureAwait(false);
            return;
        }

        if (!int.TryParse(body.UniqueId, out var playerId))
        {
            await response.WriteErrorAsync(ctx, 400, "invalid_unique_id", ct).ConfigureAwait(false);
            return;
        }

        // Verify this avatar belongs to the authenticated account
        var owned = await players
            .GetAvatarsForAccountAsync(accountId.Value, ct)
            .ConfigureAwait(false);

        if (!owned.Exists(a => a.UniqueId == body.UniqueId))
        {
            await response.WriteErrorAsync(ctx, 403, "avatar_not_owned", ct).ConfigureAwait(false);
            return;
        }

        sessions.SetSelectedPlayer(sessionId, playerId);
        await response.WriteJsonAsync(ctx, 200, new { }, null, ct).ConfigureAwait(false);
    }

    private async Task HandleSsoTokenAsync(
        HttpListenerContext ctx,
        int? accountId,
        string? sessionId,
        string ip,
        CancellationToken ct
    )
    {
        if (accountId is null)
        {
            await response.WriteErrorAsync(ctx, 401, "unauthorized", ct).ConfigureAwait(false);
            return;
        }

        // Prefer the avatar selected via /api/user/avatars/select this session,
        // then fall back to the uniqueId query param, then to the first avatar on the account.
        var selectedFromSession = sessions.GetSelectedPlayer(sessionId);
        var uniqueIdRaw = ctx.Request.QueryString["uniqueId"];
        int playerId;

        if (selectedFromSession.HasValue)
        {
            playerId = selectedFromSession.Value;
        }
        else if (!string.IsNullOrWhiteSpace(uniqueIdRaw) && int.TryParse(uniqueIdRaw, out var pid))
        {
            playerId = pid;
        }
        else
        {
            // Fall back to first avatar on the account
            var list = await players
                .GetAvatarsForAccountAsync(accountId.Value, ct)
                .ConfigureAwait(false);

            if (list.Count == 0)
            {
                await response
                    .WriteErrorAsync(ctx, 404, "pocket.auth.no_avatars", ct)
                    .ConfigureAwait(false);
                return;
            }

            if (!int.TryParse(list[0].UniqueId, out playerId))
            {
                await response.WriteErrorAsync(ctx, 500, "internal", ct).ConfigureAwait(false);
                return;
            }
        }

        var (success, ticket, error) = await auth
            .GetSsoTokenAsync(playerId, ip, ct)
            .ConfigureAwait(false);

        if (!success)
        {
            await response
                .WriteJsonAsync(ctx, 403, new { error }, null, ct)
                .ConfigureAwait(false);
            return;
        }

        await response
            .WriteJsonAsync(ctx, 200, new { ssoToken = ticket }, null, ct)
            .ConfigureAwait(false);
    }

    private async Task HandleNameCheckAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        var body = await ReadBodyAsync<NameRequest>(ctx.Request, ct).ConfigureAwait(false);

        if (body is null || string.IsNullOrWhiteSpace(body.Name))
        {
            await response.WriteErrorAsync(ctx, 400, "invalid_request", ct).ConfigureAwait(false);
            return;
        }

        var available = await players.NameAvailableAsync(body.Name, ct).ConfigureAwait(false);

        await response
            .WriteJsonAsync(ctx, 200, new { name = body.Name, valid = available }, null, ct)
            .ConfigureAwait(false);
    }

    private async Task HandleNameSelectAsync(
        HttpListenerContext ctx,
        int? accountId,
        CancellationToken ct
    )
    {
        if (accountId is null)
        {
            await response.WriteErrorAsync(ctx, 401, "unauthorized", ct).ConfigureAwait(false);
            return;
        }

        var body = await ReadBodyAsync<NameSelectRequest>(ctx.Request, ct).ConfigureAwait(false);

        if (body is null || string.IsNullOrWhiteSpace(body.Name) || body.PlayerId <= 0)
        {
            await response.WriteErrorAsync(ctx, 400, "invalid_request", ct).ConfigureAwait(false);
            return;
        }

        var ok = await players.SetNameAsync(body.PlayerId, body.Name, ct).ConfigureAwait(false);

        if (!ok)
        {
            await response
                .WriteJsonAsync(ctx, 409, new { error = "pocket.auth.name_taken" }, null, ct)
                .ConfigureAwait(false);
            return;
        }

        await response
            .WriteJsonAsync(ctx, 200, new { name = body.Name }, null, ct)
            .ConfigureAwait(false);
    }

    private async Task HandleSaveFigureAsync(
        HttpListenerContext ctx,
        int? accountId,
        CancellationToken ct
    )
    {
        if (accountId is null)
        {
            await response.WriteErrorAsync(ctx, 401, "unauthorized", ct).ConfigureAwait(false);
            return;
        }

        var body = await ReadBodyAsync<SaveFigureRequest>(ctx.Request, ct).ConfigureAwait(false);

        if (body is null || string.IsNullOrWhiteSpace(body.FigureString) || body.PlayerId <= 0)
        {
            await response.WriteErrorAsync(ctx, 400, "invalid_request", ct).ConfigureAwait(false);
            return;
        }

        var ok = await players
            .SaveFigureAsync(body.PlayerId, body.FigureString, body.Gender ?? "M", ct)
            .ConfigureAwait(false);

        await response
            .WriteJsonAsync(ctx, ok ? 200 : 404, new { }, null, ct)
            .ConfigureAwait(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? ReadSessionCookie(HttpListenerRequest request)
    {
        var cookie = request.Cookies["habbo-web-session"];
        return cookie?.Value;
    }

    private static async Task<T?> ReadBodyAsync<T>(
        HttpListenerRequest request,
        CancellationToken ct
    )
        where T : class
    {
        if (request.ContentLength64 > MaxBodyBytes)
            return null;

        try
        {
            using var reader = new StreamReader(
                request.InputStream,
                request.ContentEncoding ?? Encoding.UTF8
            );
            var json = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(json, BodyJson);
        }
        catch
        {
            return null;
        }
    }

    // ── Request DTOs ──────────────────────────────────────────────────────────

    private sealed record LoginRequest(string? Email, string? Password);

    private sealed record RegisterRequest(
        string? Email,
        string? Password,
        string? PasswordRepeated
    );

    private sealed record CreateAvatarRequest(string? Name, string? Figure, string? Gender);

    private sealed record SelectAvatarRequest(string? UniqueId);

    private sealed record NameRequest(string? Name);

    private sealed record NameSelectRequest(string? Name, int PlayerId);

    private sealed record SaveFigureRequest(string? FigureString, string? Gender, int PlayerId);
}
