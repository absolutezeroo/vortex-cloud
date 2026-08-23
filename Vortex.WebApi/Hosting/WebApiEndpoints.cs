using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Primitives.Authentication;
using Vortex.Primitives.Hosting;
using Vortex.WebApi.Http;
using Vortex.WebApi.Services;
using Vortex.WebApi.Session;

namespace Vortex.WebApi.Hosting;

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

        app.MapGet(
                "/health",
                async (
                    RequiredServiceGuard guard,
                    IDbContextFactory<VortexDbContext> dbCtxFactory,
                    CancellationToken ct
                ) =>
                {
                    bool databaseUp;

                    try
                    {
                        await using VortexDbContext dbCtx = await dbCtxFactory
                            .CreateDbContextAsync(ct)
                            .ConfigureAwait(false);

                        databaseUp = await dbCtx.Database.CanConnectAsync(ct).ConfigureAwait(false);
                    }
                    catch
                    {
                        // A probe failure is a health signal, not an error to propagate - report it
                        // as part of the response body instead of a 500.
                        databaseUp = false;
                    }

                    string status =
                        !databaseUp ? "Unhealthy"
                        : guard.IsDegraded ? "Degraded"
                        : "Healthy";

                    return Results.Json(
                        new
                        {
                            status,
                            database = databaseUp ? "up" : "down",
                            degradedServices = guard.DegradedServices,
                        },
                        statusCode: status == "Unhealthy"
                            ? StatusCodes.Status503ServiceUnavailable
                            : StatusCodes.Status200OK
                    );
                }
            )
            .WithName("Health")
            .WithSummary(
                "Liveness/readiness probe: database connectivity and RequiredServiceGuard's "
                    + "degraded-service state (OPS-02)."
            )
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
                    IWebApiPlayerService players,
                    CancellationToken ct
                ) =>
                {
                    if (body is null || !body.IsValid)
                    {
                        return Error(
                            StatusCodes.Status400BadRequest,
                            "pocket.auth.missing_credentials"
                        );
                    }

                    (bool success, string? sessionId, int accountId, string? error) =
                        await auth.LoginAsync(body.Email!, body.Password!, body.Code, ct)
                            .ConfigureAwait(false);

                    if (!success)
                    {
                        // mfa_required is not a failure the visitor can do anything about except
                        // send a code, so it rides the same 401 as the rest: the client tells them
                        // apart by the error string, and neither ever carries a session.
                        return Results.Json(
                            new { error },
                            statusCode: StatusCodes.Status401Unauthorized
                        );
                    }

                    ctx.IssueSessionCookie(sessionId!);

                    System.Collections.Generic.List<AvatarInfo> avatars = await players
                        .GetAvatarsForAccountAsync(accountId, ct)
                        .ConfigureAwait(false);

                    return Results.Json(new { requiresOnboarding = avatars.Count == 0 });
                }
            )
            .RequireRateLimiting(LoginRateLimitPolicy)
            .WithName("Login")
            .WithSummary("Authenticate an account and start a web session.")
            .WithTags(TagAuth);

        app.MapPost(
                "/api/public/authentication/password",
                async (
                    HttpContext ctx,
                    ChangePasswordRequest body,
                    WebApiSessionStore sessions,
                    IAccountPasswordService passwords,
                    CancellationToken ct
                ) =>
                {
                    int? accountId = ctx.AccountId(sessions);

                    if (accountId is null)
                    {
                        return Error(
                            StatusCodes.Status401Unauthorized,
                            "pocket.auth.not_authenticated"
                        );
                    }

                    if (body is null || !body.IsValid)
                    {
                        return Error(
                            StatusCodes.Status400BadRequest,
                            "pocket.auth.missing_credentials"
                        );
                    }

                    PasswordChangeResult result = await passwords
                        .ChangeAsync(
                            accountId.Value,
                            body.CurrentPassword!,
                            body.NewPassword!,
                            body.Code,
                            ct
                        )
                        .ConfigureAwait(false);

                    if (!result.Succeeded)
                    {
                        return Error(
                            StatusCodes.Status400BadRequest,
                            result.Outcome switch
                            {
                                PasswordChangeOutcome.MfaRequired => "pocket.auth.mfa_required",
                                PasswordChangeOutcome.InvalidCode => "pocket.auth.invalid_code",
                                PasswordChangeOutcome.TooShort => "pocket.auth.password_too_short",
                                _ => "pocket.auth.wrong_password",
                            }
                        );
                    }

                    // Every session of the account is gone, this one included. Clearing the cookie
                    // is what stops the browser presenting a token that no longer resolves.
                    ctx.ClearSessionCookie();

                    return Results.Json(new { sessionsRevoked = result.SessionsRevoked });
                }
            )
            .WithName("ChangePassword")
            .WithSummary("Change the signed-in account's password and end every session it has.")
            .WithTags(TagAuth);

        app.MapPost(
                "/api/public/authentication/logout",
                (HttpContext ctx, WebApiSessionStore sessions) =>
                {
                    string? sessionId = ctx.SessionId();

                    if (sessionId is not null)
                    {
                        sessions.RemoveSession(sessionId);
                    }

                    ctx.ClearSessionCookie();

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
                    {
                        return Error(
                            StatusCodes.Status400BadRequest,
                            "pocket.auth.missing_credentials"
                        );
                    }

                    (bool success, int accountId, string? error) = await auth.RegisterAsync(
                            body.Email!,
                            body.Password!,
                            ct
                        )
                        .ConfigureAwait(false);

                    if (!success)
                    {
                        return Results.Json(
                            new { error },
                            statusCode: StatusCodes.Status409Conflict
                        );
                    }

                    // An account created a moment ago cannot have a second factor yet, so there is
                    // no code to pass on.
                    (bool loginOk, string? sessionId, _, _) = await auth.LoginAsync(
                            body.Email!,
                            body.Password!,
                            code: null,
                            ct
                        )
                        .ConfigureAwait(false);

                    if (loginOk && sessionId is not null)
                    {
                        ctx.IssueSessionCookie(sessionId);
                    }

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
                    {
                        return Unauthorized();
                    }

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
                    {
                        return Unauthorized();
                    }

                    if (body is null || !body.IsValid)
                    {
                        return Error(StatusCodes.Status400BadRequest, "invalid_request");
                    }

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
                    {
                        return Results.Json(
                            new { error },
                            statusCode: StatusCodes.Status409Conflict
                        );
                    }

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
                    {
                        return Unauthorized();
                    }

                    if (body is null || !body.IsValid)
                    {
                        return Error(StatusCodes.Status400BadRequest, "invalid_request");
                    }

                    if (!int.TryParse(body.UniqueId, out int playerId))
                    {
                        return Error(StatusCodes.Status400BadRequest, "invalid_unique_id");
                    }

                    System.Collections.Generic.List<AvatarInfo> owned = await players
                        .GetAvatarsForAccountAsync(accountId.Value, ct)
                        .ConfigureAwait(false);

                    if (!owned.Exists(a => a.UniqueId == body.UniqueId))
                    {
                        return Error(StatusCodes.Status403Forbidden, "avatar_not_owned");
                    }

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
                    {
                        return Unauthorized();
                    }

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
                        System.Collections.Generic.List<AvatarInfo> ownedForSso = await players
                            .GetAvatarsForAccountAsync(accountId.Value, ct)
                            .ConfigureAwait(false);

                        if (!ownedForSso.Exists(a => a.UniqueId == uniqueId))
                        {
                            return Error(StatusCodes.Status403Forbidden, "avatar_not_owned");
                        }

                        playerId = pid;
                    }
                    else
                    {
                        System.Collections.Generic.List<AvatarInfo> list = await players
                            .GetAvatarsForAccountAsync(accountId.Value, ct)
                            .ConfigureAwait(false);

                        if (list.Count == 0)
                        {
                            return Error(StatusCodes.Status404NotFound, "pocket.auth.no_avatars");
                        }

                        if (!int.TryParse(list[0].UniqueId, out playerId))
                        {
                            return Error(StatusCodes.Status500InternalServerError, "internal");
                        }
                    }

                    (bool success, string? ticket, string? error) = await auth.GetSsoTokenAsync(
                            playerId,
                            ctx.RemoteIp(),
                            ct
                        )
                        .ConfigureAwait(false);

                    if (!success)
                    {
                        return Results.Json(
                            new { error },
                            statusCode: StatusCodes.Status403Forbidden
                        );
                    }

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
                    {
                        return Unauthorized();
                    }

                    if (body is null || !body.IsValid)
                    {
                        return Error(StatusCodes.Status400BadRequest, "invalid_request");
                    }

                    System.Collections.Generic.List<AvatarInfo> ownedForFigure = await players
                        .GetAvatarsForAccountAsync(accountId.Value, ct)
                        .ConfigureAwait(false);

                    if (!ownedForFigure.Exists(a => a.UniqueId == body.PlayerId.ToString()))
                    {
                        return Error(StatusCodes.Status403Forbidden, "avatar_not_owned");
                    }

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
                    {
                        return Error(StatusCodes.Status400BadRequest, "invalid_request");
                    }

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
                    {
                        return Unauthorized();
                    }

                    if (body is null || !body.IsValid)
                    {
                        return Error(StatusCodes.Status400BadRequest, "invalid_request");
                    }

                    System.Collections.Generic.List<AvatarInfo> ownedForName = await players
                        .GetAvatarsForAccountAsync(accountId.Value, ct)
                        .ConfigureAwait(false);

                    if (!ownedForName.Exists(a => a.UniqueId == body.PlayerId.ToString()))
                    {
                        return Error(StatusCodes.Status403Forbidden, "avatar_not_owned");
                    }

                    bool ok = await players
                        .SetNameAsync(body.PlayerId, body.Name!, ct)
                        .ConfigureAwait(false);

                    if (!ok)
                    {
                        return Results.Json(
                            new { error = "pocket.auth.name_taken" },
                            statusCode: StatusCodes.Status409Conflict
                        );
                    }

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
