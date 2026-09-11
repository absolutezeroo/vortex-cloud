using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Vortex.Database.Context;
using Vortex.Primitives.Authentication;
using Vortex.Primitives.Hosting;
using Vortex.Primitives.Observability;
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
    public const string ReportRateLimitPolicy = "webapi-report";

    private const string TagPublic = "Public";
    private const string TagAuth = "Authentication";
    private const string TagUser = "User";
    private const string TagNewUser = "NewUser";
    private const string TagContent = "Content";

    public static void Map(WebApplication app)
    {
        MapPublic(app);
        MapProfiles(app);
        MapAuthentication(app);
        MapUser(app);
        MapNewUser(app);
        MapContent(app);
    }

    /// <summary>
    /// Public profiles, on habbo.com's own two routes: a name lookup that answers the header, and
    /// the full read keyed by the id that lookup returns. Anonymous, because a profile is a page a
    /// signed-out visitor opens — the site links to one from every friend head.
    /// </summary>
    private static void MapProfiles(WebApplication app)
    {
        app.MapGet(
                "/api/public/users",
                async Task<Results<Ok<ProfileUser>, NotFound<ApiErrorResponse>>> (
                    string? name,
                    IWebApiProfileService profiles,
                    CancellationToken ct
                ) =>
                {
                    ProfileUser? user = await profiles
                        .FindUserByNameAsync(name ?? string.Empty, ct)
                        .ConfigureAwait(false);

                    return user is null
                        ? TypedResults.NotFound(new ApiErrorResponse("user_not_found"))
                        : TypedResults.Ok(user);
                }
            )
            .WithName("UserByName")
            .WithSummary("Resolve a habbo name to its public profile header.")
            .WithTags(TagPublic);

        app.MapGet(
                "/api/public/users/{uniqueId}/profile",
                async Task<Results<Ok<PlayerProfile>, NotFound<ApiErrorResponse>>> (
                    string uniqueId,
                    IWebApiProfileService profiles,
                    CancellationToken ct
                ) =>
                {
                    // The id is a player id rendered as a string, which is what the lookup above
                    // hands out. Parsing here rather than binding an int keeps a junk id a 404 — the
                    // same answer as an id that does not exist — instead of a 400 that tells a
                    // caller they at least guessed the shape right.
                    if (!int.TryParse(uniqueId, out int playerId))
                    {
                        return TypedResults.NotFound(new ApiErrorResponse("user_not_found"));
                    }

                    PlayerProfile? profile = await profiles
                        .GetProfileAsync(playerId, ct)
                        .ConfigureAwait(false);

                    return profile is null
                        ? TypedResults.NotFound(new ApiErrorResponse("user_not_found"))
                        : TypedResults.Ok(profile);
                }
            )
            .WithName("UserProfile")
            .WithSummary("A player's badges, friends, rooms and groups.")
            .WithTags(TagPublic);

        // habbo.com's own route for one appart ("/public/rooms/:id"). The gallery beside it is an
        // ADDITION: habbo.com's is server-rendered and calls nothing, but this site is a SPA and
        // needs a list from somewhere.
        app.MapGet(
                "/api/public/rooms",
                async Task<Ok<RoomPage>> (
                    int? page,
                    int? pageSize,
                    IWebApiRoomService rooms,
                    CancellationToken ct
                ) =>
                    TypedResults.Ok(
                        await rooms
                            .GetRoomsAsync(page ?? 1, pageSize ?? 0, ct)
                            .ConfigureAwait(false)
                    )
            )
            .WithName("Rooms")
            .WithSummary("The appart gallery: the busiest visible rooms first.")
            .WithTags(TagPublic);

        app.MapGet(
                "/api/public/rooms/{id:int}",
                async Task<Results<Ok<RoomSummary>, NotFound<ApiErrorResponse>>> (
                    int id,
                    IWebApiRoomService rooms,
                    CancellationToken ct
                ) =>
                {
                    RoomSummary? room = await rooms.GetRoomAsync(id, ct).ConfigureAwait(false);

                    return room is null
                        ? TypedResults.NotFound(new ApiErrorResponse("room_not_found"))
                        : TypedResults.Ok(room);
                }
            )
            .WithName("Room")
            .WithSummary("One appart.")
            .WithTags(TagPublic);
    }

    /// <summary>
    /// The website's editorial reads — the news feed, an article, and the languages the hotel
    /// publishes in. Anonymous by design: this is what a visitor sees before signing in, and the
    /// front page must render with no session at all.
    /// </summary>
    private static void MapContent(WebApplication app)
    {
        app.MapGet(
                "/api/public/languages",
                async Task<Ok<SiteLanguages>> (
                    IWebApiArticleService articles,
                    CancellationToken ct
                ) => TypedResults.Ok(await articles.GetLanguagesAsync(ct).ConfigureAwait(false))
            )
            .WithName("Languages")
            .WithSummary("The enabled site languages and which one is the fallback.")
            .WithTags(TagContent);

        app.MapGet(
                "/api/public/articles",
                async Task<Ok<ArticleFeed>> (
                    HttpContext ctx,
                    IWebApiArticleService articles,
                    CancellationToken ct,
                    string? category = null,
                    string? lang = null,
                    int page = 1,
                    int pageSize = 0
                ) =>
                    TypedResults.Ok(
                        await articles
                            .GetFeedAsync(
                                category,
                                lang ?? ctx.AcceptedLanguages(),
                                page,
                                pageSize,
                                ct
                            )
                            .ConfigureAwait(false)
                    )
            )
            .WithName("Articles")
            .WithSummary("A page of the news feed, newest first, pinned articles ahead of it.")
            .WithTags(TagContent);

        app.MapGet(
                "/api/public/articles/{slug}",
                async Task<Results<Ok<ArticleDetail>, NotFound<ApiErrorResponse>>> (
                    HttpContext ctx,
                    string slug,
                    IWebApiArticleService articles,
                    CancellationToken ct,
                    string? lang = null
                ) =>
                {
                    ArticleDetail? article = await articles
                        .GetArticleAsync(slug, lang ?? ctx.AcceptedLanguages(), ct)
                        .ConfigureAwait(false);

                    return article is null
                        ? TypedResults.NotFound(new ApiErrorResponse("article_not_found"))
                        : TypedResults.Ok(article);
                }
            )
            .WithName("Article")
            .WithSummary("One article with its body blocks and its read-also list.")
            .WithTags(TagContent);
    }

    private static void MapPublic(WebApplication app)
    {
        app.MapGet(
                "/api/public/info/hello",
                Ok<HelloResponse> () => TypedResults.Ok(new HelloResponse("ok"))
            )
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

                    HealthResponse health = new(
                        status,
                        databaseUp ? "up" : "down",
                        guard.DegradedServices
                    );

                    // The one endpoint left on Results.Json, and the one status that cannot be
                    // typed: there is no `ServiceUnavailable<T>`, so 503-with-a-body has to be built
                    // at runtime. Both answers are declared below instead — the probe is an
                    // operations surface no generated client reads, so a declaration is the right
                    // amount of truth for it.
                    return Results.Json(
                        health,
                        statusCode: databaseUp
                            ? StatusCodes.Status200OK
                            : StatusCodes.Status503ServiceUnavailable
                    );
                }
            )
            .WithName("Health")
            .WithSummary(
                "Liveness/readiness probe: database connectivity and RequiredServiceGuard's "
                    + "degraded-service state (OPS-02)."
            )
            .WithTags(TagPublic)
            .Produces<HealthResponse>()
            .Produces<HealthResponse>(StatusCodes.Status503ServiceUnavailable);
    }

    private static void MapAuthentication(WebApplication app)
    {
        app.MapPost(
                "/api/public/authentication/login",
                async Task<
                    Results<Ok<LoginResponse>, BadRequest<ApiErrorResponse>, UnauthorizedError>
                > (
                    HttpContext ctx,
                    LoginRequest body,
                    IWebApiAuthService auth,
                    IWebApiPlayerService players,
                    CancellationToken ct
                ) =>
                {
                    if (body is null || !body.IsValid)
                    {
                        return TypedResults.BadRequest(
                            new ApiErrorResponse("pocket.auth.missing_credentials")
                        );
                    }

                    (bool success, string? sessionId, int accountId, string? error) =
                        await auth.LoginAsync(body.Email!, body.Password!, body.Code, ct)
                            .ConfigureAwait(false);

                    if (!success)
                    {
                        // mfa_required is not a failure the visitor can do anything about except
                        // send a code, so it rides the same 401 as the rest: the client tells them
                        // apart by the error string, and neither ever carries a session. That string
                        // is why the 401 needs a body at all — see UnauthorizedError.
                        return new UnauthorizedError(error ?? "pocket.auth.invalid_login");
                    }

                    ctx.IssueSessionCookie(sessionId!);

                    System.Collections.Generic.List<AvatarInfo> avatars = await players
                        .GetAvatarsForAccountAsync(accountId, ct)
                        .ConfigureAwait(false);

                    return TypedResults.Ok(new LoginResponse(avatars.Count == 0));
                }
            )
            .RequireRateLimiting(LoginRateLimitPolicy)
            .WithName("Login")
            .WithSummary("Authenticate an account and start a web session.")
            .WithTags(TagAuth);

        app.MapPost(
                "/api/public/authentication/password",
                async Task<
                    Results<
                        Ok<PasswordChangeResponse>,
                        BadRequest<ApiErrorResponse>,
                        UnauthorizedError
                    >
                > (
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
                        return new UnauthorizedError("pocket.auth.not_authenticated");
                    }

                    if (body is null || !body.IsValid)
                    {
                        return TypedResults.BadRequest(
                            new ApiErrorResponse("pocket.auth.missing_credentials")
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
                        return TypedResults.BadRequest(
                            new ApiErrorResponse(
                                result.Outcome switch
                                {
                                    PasswordChangeOutcome.MfaRequired => "pocket.auth.mfa_required",
                                    PasswordChangeOutcome.InvalidCode => "pocket.auth.invalid_code",
                                    PasswordChangeOutcome.TooShort =>
                                        "pocket.auth.password_too_short",
                                    _ => "pocket.auth.wrong_password",
                                }
                            )
                        );
                    }

                    // Every session of the account is gone, this one included. Clearing the cookie
                    // is what stops the browser presenting a token that no longer resolves.
                    ctx.ClearSessionCookie();

                    return TypedResults.Ok(new PasswordChangeResponse(result.SessionsRevoked));
                }
            )
            .WithName("ChangePassword")
            .WithSummary("Change the signed-in account's password and end every session it has.")
            .WithTags(TagAuth);

        app.MapPost(
                "/api/public/authentication/logout",
                Ok<EmptyResponse> (HttpContext ctx, WebApiSessionStore sessions) =>
                {
                    string? sessionId = ctx.SessionId();

                    if (sessionId is not null)
                    {
                        sessions.RemoveSession(sessionId);
                    }

                    ctx.ClearSessionCookie();

                    return TypedResults.Ok(EmptyResponse.Instance);
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
                async Task<
                    Results<
                        Ok<RegistrationResponse>,
                        BadRequest<ApiErrorResponse>,
                        Conflict<ApiErrorResponse>
                    >
                > (
                    HttpContext ctx,
                    RegisterRequest body,
                    IWebApiAuthService auth,
                    CancellationToken ct
                ) =>
                {
                    if (body is null || !body.IsValid)
                    {
                        return TypedResults.BadRequest(
                            new ApiErrorResponse("pocket.auth.missing_credentials")
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
                        return TypedResults.Conflict(new ApiErrorResponse(error ?? "email_taken"));
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

                    return TypedResults.Ok(new RegistrationResponse(accountId));
                }
            )
            .RequireRateLimiting(RegistrationRateLimitPolicy)
            .WithName("Register")
            .WithSummary("Create a new account and auto-start a web session.")
            .WithTags(TagAuth);

        app.MapGet(
                "/api/user/purse",
                async Task<
                    Results<Ok<PlayerPurse>, NotFound<ApiErrorResponse>, UnauthorizedError>
                > (
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

                    // The selected avatar when there is one, and otherwise the account's first —
                    // the same fallback the SSO ticket route makes, because a session that has not
                    // been through the avatar picker still has a wallet to show.
                    int? playerId = sessions.GetSelectedPlayer(ctx.SessionId());

                    if (playerId is null)
                    {
                        System.Collections.Generic.List<AvatarInfo> owned = await players
                            .GetAvatarsForAccountAsync(accountId.Value, ct)
                            .ConfigureAwait(false);

                        if (owned.Count == 0)
                        {
                            return TypedResults.NotFound(
                                new ApiErrorResponse("pocket.auth.no_avatars")
                            );
                        }

                        playerId = int.Parse(owned[0].UniqueId, CultureInfo.InvariantCulture);
                    }

                    PlayerPurse? purse = await players
                        .GetPurseAsync(playerId.Value, ct)
                        .ConfigureAwait(false);

                    return purse is null
                        ? TypedResults.NotFound(new ApiErrorResponse("pocket.auth.no_avatars"))
                        : TypedResults.Ok(purse);
                }
            )
            .WithName("GetPurse")
            .WithSummary("The selected avatar's credits, diamonds, duckets and subscriptions.")
            .WithTags(TagUser);

        app.MapGet(
                "/api/user/avatars",
                // Also the identity probe: the 401 in this union is what the site reads as "signed
                // out", which is why it has no dedicated route.
                async Task<
                    Results<Ok<System.Collections.Generic.List<AvatarInfo>>, UnauthorizedError>
                > (
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

                    return TypedResults.Ok(
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
                async Task<
                    Results<
                        Ok<System.Collections.Generic.List<AvatarInfo>>,
                        BadRequest<ApiErrorResponse>,
                        Conflict<ApiErrorResponse>,
                        UnauthorizedError
                    >
                > (
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
                        return TypedResults.BadRequest(new ApiErrorResponse("invalid_request"));
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
                        return TypedResults.Conflict(
                            new ApiErrorResponse(error ?? "invalid_request")
                        );
                    }

                    return TypedResults.Ok(
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
                async Task<
                    Results<
                        Ok<EmptyResponse>,
                        BadRequest<ApiErrorResponse>,
                        UnauthorizedError,
                        ForbiddenError
                    >
                > (
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
                        return TypedResults.BadRequest(new ApiErrorResponse("invalid_request"));
                    }

                    if (!int.TryParse(body.UniqueId, out int playerId))
                    {
                        return TypedResults.BadRequest(new ApiErrorResponse("invalid_unique_id"));
                    }

                    System.Collections.Generic.List<AvatarInfo> owned = await players
                        .GetAvatarsForAccountAsync(accountId.Value, ct)
                        .ConfigureAwait(false);

                    if (!owned.Exists(a => a.UniqueId == body.UniqueId))
                    {
                        return new ForbiddenError("avatar_not_owned");
                    }

                    sessions.SetSelectedPlayer(ctx.SessionId(), playerId);

                    return TypedResults.Ok(EmptyResponse.Instance);
                }
            )
            .WithName("SelectAvatar")
            .WithSummary("Select the avatar used for the next SSO token.")
            .WithTags(TagUser);

        // What the player saw and the server did not. Every other observability surface here
        // records something the hotel noticed itself: the error grouping needs an exception, the
        // metrics need a counter, the audit trail needs an action somebody invoked. A room that
        // renders blank throws nothing, increments nothing and invokes nothing — it is only a bug
        // because a person says so, and this is the route that lets them.
        //
        // It writes an audit record rather than opening a table of its own. An audit event already
        // is "who, where, under which correlation id, with what payload", the investigation UI
        // already reads it, and a second store would be a second thing to back up, page and
        // retain. The category is what keeps the reports findable — see AuditCategory.PlayerReport.
        //
        // Authenticated, deliberately. An anonymous report cannot be replied to, cannot be
        // correlated with what that account was doing, and is a spam surface the rate limit alone
        // would have to hold shut.
        app.MapPost(
                "/api/user/reports",
                Results<Accepted<EmptyResponse>, BadRequest<ApiErrorResponse>, UnauthorizedError> (
                    HttpContext ctx,
                    SubmitReportRequest body,
                    WebApiSessionStore sessions,
                    IAuditSink audit
                ) =>
                {
                    int? accountId = ctx.AccountId(sessions);

                    if (accountId is null)
                    {
                        return Unauthorized();
                    }

                    if (body is null || !body.IsValid)
                    {
                        return TypedResults.BadRequest(new ApiErrorResponse("invalid_request"));
                    }

                    int? playerId = sessions.GetSelectedPlayer(ctx.SessionId());

                    audit.Emit(
                        new AuditEvent
                        {
                            Category = AuditCategory.PlayerReport,
                            Action = "player.bug_report",
                            Severity = AuditSeverity.Notice,
                            Result = AuditResult.Success,
                            ActorPlayerId = playerId,
                            RoomId = body.RoomId,

                            // Null for the same reason DashboardAuditEmitter leaves it null: the
                            // IP hashing secret lives behind the authentication module, and a
                            // second hashing scheme here would produce hashes that match nothing
                            // in the rest of the trail.
                            IpHash = null,
                            Data = JsonSerializer.Serialize(
                                new
                                {
                                    accountId = accountId.Value,
                                    message = body.Message,
                                    page = body.Page,
                                    clientVersion = body.ClientVersion,
                                    console = body.Console,
                                }
                            ),
                        }
                    );

                    // 202, not 200: Emit is a non-blocking enqueue, so the row is not written yet
                    // and claiming otherwise would be a lie the client could catch.
                    return TypedResults.Accepted((string?)null, EmptyResponse.Instance);
                }
            )
            .RequireRateLimiting(ReportRateLimitPolicy)
            .WithName("SubmitReport")
            .WithSummary("File a player bug report against the current session.")
            .WithTags(TagUser);

        app.MapGet(
                "/api/ssotoken",
                async Task<
                    Results<
                        Ok<SsoTicketResponse>,
                        NotFound<ApiErrorResponse>,
                        InternalServerError<ApiErrorResponse>,
                        UnauthorizedError,
                        ForbiddenError
                    >
                > (
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
                            return new ForbiddenError("avatar_not_owned");
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
                            return TypedResults.NotFound(
                                new ApiErrorResponse("pocket.auth.no_avatars")
                            );
                        }

                        if (!int.TryParse(list[0].UniqueId, out playerId))
                        {
                            return TypedResults.InternalServerError(
                                new ApiErrorResponse("internal")
                            );
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
                        return new ForbiddenError(error ?? "avatar_not_owned");
                    }

                    return TypedResults.Ok(new SsoTicketResponse(ticket!));
                }
            )
            .RequireRateLimiting(SsoTokenRateLimitPolicy)
            .WithName("SsoToken")
            .WithSummary("Issue a single-use SSO ticket for the selected avatar.")
            .WithTags(TagUser);

        app.MapPost(
                "/api/user/look/save",
                async Task<
                    Results<
                        Ok<EmptyResponse>,
                        NotFound<ApiErrorResponse>,
                        BadRequest<ApiErrorResponse>,
                        UnauthorizedError,
                        ForbiddenError
                    >
                > (
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
                        return TypedResults.BadRequest(new ApiErrorResponse("invalid_request"));
                    }

                    System.Collections.Generic.List<AvatarInfo> ownedForFigure = await players
                        .GetAvatarsForAccountAsync(accountId.Value, ct)
                        .ConfigureAwait(false);

                    if (!ownedForFigure.Exists(a => a.UniqueId == body.PlayerId.ToString()))
                    {
                        return new ForbiddenError("avatar_not_owned");
                    }

                    bool ok = await players
                        .SaveFigureAsync(body.PlayerId, body.FigureString!, body.Gender ?? "M", ct)
                        .ConfigureAwait(false);

                    // The 404 used to carry `{}` like the success did — the same empty body at two
                    // statuses, which told a caller nothing. It says which avatar was not found now.
                    return ok
                        ? TypedResults.Ok(EmptyResponse.Instance)
                        : TypedResults.NotFound(new ApiErrorResponse("pocket.auth.no_avatars"));
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
                async Task<Results<Ok<NameCheckResponse>, BadRequest<ApiErrorResponse>>> (
                    NameRequest body,
                    IWebApiPlayerService players,
                    CancellationToken ct
                ) =>
                {
                    if (body is null || !body.IsValid)
                    {
                        return TypedResults.BadRequest(new ApiErrorResponse("invalid_request"));
                    }

                    bool available = await players
                        .NameAvailableAsync(body.Name!, ct)
                        .ConfigureAwait(false);

                    return TypedResults.Ok(new NameCheckResponse(body.Name!, available));
                }
            )
            .WithName("NameCheck")
            .WithSummary("Check whether a player name is available.")
            .WithTags(TagNewUser);

        app.MapPost(
                "/api/newuser/name/select",
                async Task<
                    Results<
                        Ok<NameSelectResponse>,
                        BadRequest<ApiErrorResponse>,
                        Conflict<ApiErrorResponse>,
                        UnauthorizedError,
                        ForbiddenError
                    >
                > (
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
                        return TypedResults.BadRequest(new ApiErrorResponse("invalid_request"));
                    }

                    System.Collections.Generic.List<AvatarInfo> ownedForName = await players
                        .GetAvatarsForAccountAsync(accountId.Value, ct)
                        .ConfigureAwait(false);

                    if (!ownedForName.Exists(a => a.UniqueId == body.PlayerId.ToString()))
                    {
                        return new ForbiddenError("avatar_not_owned");
                    }

                    bool ok = await players
                        .SetNameAsync(body.PlayerId, body.Name!, ct)
                        .ConfigureAwait(false);

                    if (!ok)
                    {
                        return TypedResults.Conflict(
                            new ApiErrorResponse("pocket.auth.name_taken")
                        );
                    }

                    return TypedResults.Ok(new NameSelectResponse(body.Name!));
                }
            )
            .WithName("NameSelect")
            .WithSummary("Assign a name to an owned avatar.")
            .WithTags(TagNewUser);

        app.MapPost(
                "/api/newuser/room/select",
                Ok<EmptyResponse> () => TypedResults.Ok(EmptyResponse.Instance)
            )
            .WithName("RoomSelect")
            .WithSummary("Onboarding room selection (currently a no-op).")
            .WithTags(TagNewUser);
    }

    private static UnauthorizedError Unauthorized() => new("unauthorized");
}
