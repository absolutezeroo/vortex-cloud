using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Turbo.WebApi.Http;
using Turbo.WebApi.Services;
using Turbo.WebApi.Session;

namespace Turbo.WebApi.Hosting;

/// <summary>
/// Maps the client-facing web API onto minimal-API endpoints — one handler per route — preserving the
/// exact paths, methods, status codes and response shapes of the previous <c>HttpListener</c>
/// dispatcher. Authentication is the same cookie-backed session model (see
/// <see cref="WebApiHttpContextExtensions"/>); the sensitive endpoints declare a named rate-limiting
/// policy. Every endpoint is tagged for Swagger grouping.
/// </summary>
internal static class WebApiEndpoints
{
    public const string LoginRateLimitPolicy = "webapi-login";
    public const string RegistrationRateLimitPolicy = "webapi-registration";
    public const string SsoTokenRateLimitPolicy = "webapi-ssotoken";

    private const string TagPublic = "Public";
    private const string TagAuth = "Authentication";
    private const string TagUser = "User";
    private const string TagNewUser = "NewUser";

    public static void Map(WebApplication app)
    {
        MapPublic(app);
        MapAuthentication(app);
        MapUser(app);
        MapNewUser(app);
    }

    private static void MapPublic(WebApplication app)
    {
        app.MapGet("/api/public/info/hello", () => Results.Json(new { status = "ok" }))
            .WithName("Hello")
            .WithSummary("Server liveness probe used by the onboarding client.")
            .WithTags(TagPublic);
    }

    private static void MapAuthentication(WebApplication app)
    {
        app.MapPost(
                "/api/public/authentication/login",
                async (
                    HttpContext ctx,
                    LoginRequest body,
                    IWebApiAuthService auth,
                    CancellationToken ct
                ) =>
                {
                    if (body is null || !body.IsValid)
                        return Error(
                            StatusCodes.Status400BadRequest,
                            "pocket.auth.missing_credentials"
                        );

                    (bool success, string? sessionId, string? error) = await auth.LoginAsync(
                            body.Email!,
                            body.Password!,
                            ct
                        )
                        .ConfigureAwait(false);

                    if (!success)
                        return Results.Json(
                            new { error },
                            statusCode: StatusCodes.Status401Unauthorized
                        );

                    ctx.IssueSessionCookie(sessionId!);

                    return Results.Json(new { });
                }
            )
            .RequireRateLimiting(LoginRateLimitPolicy)
            .WithName("Login")
            .WithSummary("Authenticate an account and start a web session.")
            .WithTags(TagAuth);

        app.MapPost(
                "/api/public/authentication/logout",
                (HttpContext ctx, WebApiSessionStore sessions) =>
                {
                    string? sessionId = ctx.SessionId();

                    if (sessionId is not null)
                        sessions.RemoveSession(sessionId);

                    ctx.Response.Cookies.Delete(
                        WebApiHttpContextExtensions.SessionCookieName,
                        new CookieOptions { Path = "/" }
                    );

                    return Results.Json(new { });
                }
            )
            .WithName("Logout")
            .WithSummary("End the current web session.")
            .WithTags(TagAuth);
    }

    private static void MapUser(WebApplication app)
    {
        app.MapPost(
                "/api/public/registration/new",
                async (
                    HttpContext ctx,
                    RegisterRequest body,
                    IWebApiAuthService auth,
                    CancellationToken ct
                ) =>
                {
                    if (body is null || !body.IsValid)
                        return Error(
                            StatusCodes.Status400BadRequest,
                            "pocket.auth.missing_credentials"
                        );

                    (bool success, int accountId, string? error) = await auth.RegisterAsync(
                            body.Email!,
                            body.Password!,
                            ct
                        )
                        .ConfigureAwait(false);

                    if (!success)
                        return Results.Json(
                            new { error },
                            statusCode: StatusCodes.Status409Conflict
                        );

                    (bool loginOk, string? sessionId, string? _) = await auth.LoginAsync(
                            body.Email!,
                            body.Password!,
                            ct
                        )
                        .ConfigureAwait(false);

                    if (loginOk && sessionId is not null)
                        ctx.IssueSessionCookie(sessionId);

                    return Results.Json(new { id = accountId });
                }
            )
            .RequireRateLimiting(RegistrationRateLimitPolicy)
            .WithName("Register")
            .WithSummary("Create a new account and auto-start a web session.")
            .WithTags(TagAuth);

        app.MapGet(
                "/api/user/avatars",
                async (
                    HttpContext ctx,
                    WebApiSessionStore sessions,
                    IWebApiPlayerService players,
                    CancellationToken ct
                ) =>
                {
                    int? accountId = ctx.AccountId(sessions);

                    if (accountId is null)
                        return Unauthorized();

                    return Results.Json(
                        await players
                            .GetAvatarsForAccountAsync(accountId.Value, ct)
                            .ConfigureAwait(false)
                    );
                }
            )
            .WithName("GetAvatars")
            .WithSummary("List the avatars owned by the authenticated account.")
            .WithTags(TagUser);

        app.MapPost(
                "/api/user/avatars",
                async (
                    HttpContext ctx,
                    CreateAvatarRequest body,
                    WebApiSessionStore sessions,
                    IWebApiPlayerService players,
                    CancellationToken ct
                ) =>
                {
                    int? accountId = ctx.AccountId(sessions);

                    if (accountId is null)
                        return Unauthorized();

                    if (body is null || !body.IsValid)
                        return Error(StatusCodes.Status400BadRequest, "invalid_request");

                    (bool success, int _, string? error) = await players
                        .CreateAvatarAsync(
                            accountId.Value,
                            body.Name!,
                            body.Figure ?? string.Empty,
                            body.Gender ?? "M",
                            ct
                        )
                        .ConfigureAwait(false);

                    if (!success)
                        return Results.Json(
                            new { error },
                            statusCode: StatusCodes.Status409Conflict
                        );

                    return Results.Json(
                        await players
                            .GetAvatarsForAccountAsync(accountId.Value, ct)
                            .ConfigureAwait(false)
                    );
                }
            )
            .WithName("CreateAvatar")
            .WithSummary("Create an avatar and return the refreshed avatar list.")
            .WithTags(TagUser);

        app.MapPost(
                "/api/user/avatars/select",
                async (
                    HttpContext ctx,
                    SelectAvatarRequest body,
                    WebApiSessionStore sessions,
                    IWebApiPlayerService players,
                    CancellationToken ct
                ) =>
                {
                    int? accountId = ctx.AccountId(sessions);

                    if (accountId is null)
                        return Unauthorized();

                    if (body is null || !body.IsValid)
                        return Error(StatusCodes.Status400BadRequest, "invalid_request");

                    if (!int.TryParse(body.UniqueId, out int playerId))
                        return Error(StatusCodes.Status400BadRequest, "invalid_unique_id");

                    System.Collections.Generic.List<AvatarInfo> owned = await players
                        .GetAvatarsForAccountAsync(accountId.Value, ct)
                        .ConfigureAwait(false);

                    if (!owned.Exists(a => a.UniqueId == body.UniqueId))
                        return Error(StatusCodes.Status403Forbidden, "avatar_not_owned");

                    sessions.SetSelectedPlayer(ctx.SessionId(), playerId);

                    return Results.Json(new { });
                }
            )
            .WithName("SelectAvatar")
            .WithSummary("Select the avatar used for the next SSO token.")
            .WithTags(TagUser);

        app.MapGet(
                "/api/ssotoken",
                async (
                    HttpContext ctx,
                    string? uniqueId,
                    WebApiSessionStore sessions,
                    IWebApiPlayerService players,
                    IWebApiAuthService auth,
                    CancellationToken ct
                ) =>
                {
                    int? accountId = ctx.AccountId(sessions);

                    if (accountId is null)
                        return Unauthorized();

                    int? selectedFromSession = sessions.GetSelectedPlayer(ctx.SessionId());
                    int playerId;

                    if (selectedFromSession.HasValue)
                    {
                        playerId = selectedFromSession.Value;
                    }
                    else if (
                        !string.IsNullOrWhiteSpace(uniqueId) && int.TryParse(uniqueId, out int pid)
                    )
                    {
                        playerId = pid;
                    }
                    else
                    {
                        System.Collections.Generic.List<AvatarInfo> list = await players
                            .GetAvatarsForAccountAsync(accountId.Value, ct)
                            .ConfigureAwait(false);

                        if (list.Count == 0)
                            return Error(StatusCodes.Status404NotFound, "pocket.auth.no_avatars");

                        if (!int.TryParse(list[0].UniqueId, out playerId))
                            return Error(StatusCodes.Status500InternalServerError, "internal");
                    }

                    (bool success, string? ticket, string? error) = await auth.GetSsoTokenAsync(
                            playerId,
                            ctx.RemoteIp(),
                            ct
                        )
                        .ConfigureAwait(false);

                    if (!success)
                        return Results.Json(
                            new { error },
                            statusCode: StatusCodes.Status403Forbidden
                        );

                    return Results.Json(new { ssoToken = ticket });
                }
            )
            .RequireRateLimiting(SsoTokenRateLimitPolicy)
            .WithName("SsoToken")
            .WithSummary("Issue a single-use SSO ticket for the selected avatar.")
            .WithTags(TagUser);

        app.MapPost(
                "/api/user/look/save",
                async (
                    HttpContext ctx,
                    SaveFigureRequest body,
                    WebApiSessionStore sessions,
                    IWebApiPlayerService players,
                    CancellationToken ct
                ) =>
                {
                    int? accountId = ctx.AccountId(sessions);

                    if (accountId is null)
                        return Unauthorized();

                    if (body is null || !body.IsValid)
                        return Error(StatusCodes.Status400BadRequest, "invalid_request");

                    bool ok = await players
                        .SaveFigureAsync(body.PlayerId, body.FigureString!, body.Gender ?? "M", ct)
                        .ConfigureAwait(false);

                    return Results.Json(
                        new { },
                        statusCode: ok ? StatusCodes.Status200OK : StatusCodes.Status404NotFound
                    );
                }
            )
            .WithName("SaveFigure")
            .WithSummary("Persist the figure string for an owned avatar.")
            .WithTags(TagUser);
    }

    private static void MapNewUser(WebApplication app)
    {
        app.MapPost(
                "/api/newuser/name/check",
                async (NameRequest body, IWebApiPlayerService players, CancellationToken ct) =>
                {
                    if (body is null || !body.IsValid)
                        return Error(StatusCodes.Status400BadRequest, "invalid_request");

                    bool available = await players
                        .NameAvailableAsync(body.Name!, ct)
                        .ConfigureAwait(false);

                    return Results.Json(new { name = body.Name, valid = available });
                }
            )
            .WithName("NameCheck")
            .WithSummary("Check whether a player name is available.")
            .WithTags(TagNewUser);

        app.MapPost(
                "/api/newuser/name/select",
                async (
                    HttpContext ctx,
                    NameSelectRequest body,
                    WebApiSessionStore sessions,
                    IWebApiPlayerService players,
                    CancellationToken ct
                ) =>
                {
                    int? accountId = ctx.AccountId(sessions);

                    if (accountId is null)
                        return Unauthorized();

                    if (body is null || !body.IsValid)
                        return Error(StatusCodes.Status400BadRequest, "invalid_request");

                    bool ok = await players
                        .SetNameAsync(body.PlayerId, body.Name!, ct)
                        .ConfigureAwait(false);

                    if (!ok)
                        return Results.Json(
                            new { error = "pocket.auth.name_taken" },
                            statusCode: StatusCodes.Status409Conflict
                        );

                    return Results.Json(new { name = body.Name });
                }
            )
            .WithName("NameSelect")
            .WithSummary("Assign a name to an owned avatar.")
            .WithTags(TagNewUser);

        app.MapPost("/api/newuser/room/select", () => Results.Json(new { }))
            .WithName("RoomSelect")
            .WithSummary("Onboarding room selection (currently a no-op).")
            .WithTags(TagNewUser);
    }

    private static IResult Unauthorized() =>
        Error(StatusCodes.Status401Unauthorized, "unauthorized");

    private static IResult Error(int statusCode, string errorCode) =>
        Results.Json(new { error = errorCode }, statusCode: statusCode);
}
