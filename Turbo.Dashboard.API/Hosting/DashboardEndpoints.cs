using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Turbo.Dashboard.API.Api;
using Turbo.Dashboard.API.Infrastructure;
using Turbo.Dashboard.API.Operations;
using Turbo.Dashboard.API.Security;
using Turbo.Primitives.Permissions;

namespace Turbo.Dashboard.API.Hosting;

/// <summary>
/// Maps the dashboard HTTP surface onto minimal-API endpoints. Authentication is handled by
/// <see cref="DashboardAuthenticationHandler"/> and authorization by per-capability policies (named
/// after the capability string), so each endpoint just declares the capability it needs via
/// <c>RequireAuthorization</c>. Every endpoint is tagged for Swagger grouping.
/// </summary>
internal static class DashboardEndpoints
{
    private const string TagAuth = "Auth";
    private const string TagMonitoring = "Monitoring";
    private const string TagForensics = "Forensics";
    private const string TagEconomy = "Economy";
    private const string TagDirectory = "Directory";
    private const string TagOperations = "Operations";

    public static void MapAuth(WebApplication app)
    {
        app.MapPost(
                "/api/login",
                async (HttpContext ctx, LoginRequest body, DashboardAuthService auth) =>
                {
                    if (
                        body is null
                        || string.IsNullOrWhiteSpace(body.Email)
                        || string.IsNullOrEmpty(body.Password)
                    )
                        return Results.BadRequest(new { error = "invalid_request" });

                    var result = await auth.LoginAsync(
                            body.Email,
                            body.Password,
                            ctx.RequestAborted
                        )
                        .ConfigureAwait(false);

                    return result.Outcome switch
                    {
                        DashboardLoginOutcome.Authenticated => IssueSession(ctx, result),
                        DashboardLoginOutcome.Forbidden => Results.Json(
                            new { error = "forbidden" },
                            statusCode: StatusCodes.Status403Forbidden
                        ),
                        _ => Results.Json(
                            new { error = "invalid_credentials" },
                            statusCode: StatusCodes.Status401Unauthorized
                        ),
                    };
                }
            )
            .AllowAnonymous()
            .WithName("Login")
            .WithSummary("Authenticate a dashboard operator and start a session.")
            .WithTags(TagAuth);

        app.MapPost(
                "/api/logout",
                (HttpContext ctx, DashboardAuthService auth) =>
                {
                    auth.Logout(
                        ctx.Request.Cookies[DashboardAuthenticationHandler.SessionCookieName]
                    );
                    ctx.Response.Cookies.Delete(
                        DashboardAuthenticationHandler.SessionCookieName,
                        new CookieOptions { Path = "/" }
                    );
                    return Results.Ok(new { ok = true });
                }
            )
            .AllowAnonymous()
            .WithName("Logout")
            .WithSummary("End the current dashboard session.")
            .WithTags(TagAuth);

        app.MapGet(
                "/api/me",
                (HttpContext ctx) =>
                {
                    var principal = ctx.GetDashboardPrincipal();
                    return principal is null
                        ? Results.Json(
                            new { error = "unauthenticated" },
                            statusCode: StatusCodes.Status401Unauthorized
                        )
                        : Results.Ok(BuildIdentity(principal));
                }
            )
            .RequireAuthorization()
            .WithName("Identity")
            .WithSummary("The authenticated operator and their effective capabilities.")
            .WithTags(TagAuth);
    }

    public static void MapReadApi(WebApplication app, Func<DateTime> startedAtUtc)
    {
        app.MapGet(
                "/api/overview",
                (DashboardApiService api, CancellationToken ct) =>
                    Ok(api.OverviewAsync(startedAtUtc(), ct))
            )
            .RequireAuthorization(Capabilities.Dashboard.OverviewRead)
            .WithTags(TagMonitoring);

        app.MapGet(
                "/api/infrastructure",
                async (DashboardApiService api, CancellationToken ct) =>
                    Results.Ok(await api.InfrastructureAsync(ct).ConfigureAwait(false))
            )
            .RequireAuthorization(Capabilities.Dashboard.OverviewRead)
            .WithTags(TagMonitoring);

        app.MapGet(
                "/api/incidents",
                async (DashboardApiService api, CancellationToken ct) =>
                    Results.Ok(await api.IncidentsAsync(ct).ConfigureAwait(false))
            )
            .RequireAuthorization(Capabilities.Dashboard.OverviewRead)
            .WithTags(TagMonitoring);

        app.MapGet(
                "/api/packet-stats",
                (DashboardApiService api, CancellationToken ct) => Ok(api.PacketStatsAsync(ct))
            )
            .RequireAuthorization(Capabilities.Dashboard.OverviewRead)
            .WithTags(TagMonitoring);

        app.MapGet(
                "/api/audit",
                (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                    Ok(api.AuditAsync(ctx.QueryAsNameValues(), ct))
            )
            .RequireAuthorization(Capabilities.Dashboard.AuditRead)
            .WithTags(TagForensics);

        app.MapGet(
                "/api/economy",
                (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                    Ok(api.EconomyAsync(ctx.QueryAsNameValues(), ct))
            )
            .RequireAuthorization(Capabilities.Dashboard.EconomyRead)
            .WithTags(TagEconomy);

        app.MapGet(
                "/api/search",
                (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                    Ok(api.SearchAsync(ctx.QueryAsNameValues(), ct))
            )
            .RequireAuthorization(Capabilities.Dashboard.AuditRead)
            .WithTags(TagForensics);

        app.MapGet(
                "/api/players",
                (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                    Ok(api.PlayersAsync(ctx.QueryAsNameValues(), ct))
            )
            .RequireAuthorization(Capabilities.Dashboard.PlayersRead)
            .WithTags(TagDirectory);

        app.MapGet(
                "/api/furniture",
                (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                    Ok(api.FurnitureDefinitionsAsync(ctx.QueryAsNameValues(), ct))
            )
            .RequireAuthorization(Capabilities.Dashboard.FurnitureRead)
            .WithTags(TagDirectory);

        app.MapGet(
                "/api/item/{id}",
                (string id, HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                    OkNullable(api.ItemAsync(id, ctx.QueryAsNameValues(), ct))
            )
            .RequireAuthorization(Capabilities.Dashboard.AuditRead)
            .WithTags(TagForensics);

        app.MapGet(
                "/api/room/{roomId:int}",
                (int roomId, HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                    OkNullable(api.RoomTimelineAsync(roomId, ctx.QueryAsNameValues(), ct))
            )
            .RequireAuthorization(Capabilities.Dashboard.AuditRead)
            .WithTags(TagForensics);
    }

    public static void MapOperations(WebApplication app)
    {
        app.MapPost(
                "/api/ops/currency/credits",
                async (
                    HttpContext ctx,
                    GiveCreditsRequest body,
                    DashboardOperationsService ops,
                    CancellationToken ct
                ) =>
                {
                    if (
                        body is null
                        || body.PlayerId <= 0
                        || body.Amount <= 0
                        || !HasReason(body.Reason)
                    )
                        return Results.BadRequest(new { error = "invalid_request" });

                    return Results.Ok(
                        await ops.GiveCreditsAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                    );
                }
            )
            .RequireAuthorization(Capabilities.Dashboard.OpsGrantCurrency)
            .WithTags(TagOperations);

        app.MapPost(
                "/api/ops/currency/activity-points",
                async (
                    HttpContext ctx,
                    GiveActivityPointsRequest body,
                    DashboardOperationsService ops,
                    CancellationToken ct
                ) =>
                {
                    if (
                        body is null
                        || body.PlayerId <= 0
                        || body.Type < 0
                        || body.Amount <= 0
                        || !HasReason(body.Reason)
                    )
                        return Results.BadRequest(new { error = "invalid_request" });

                    return Results.Ok(
                        await ops.GiveActivityPointsAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    );
                }
            )
            .RequireAuthorization(Capabilities.Dashboard.OpsGrantCurrency)
            .WithTags(TagOperations);

        app.MapPost(
                "/api/ops/item/grant",
                async (
                    HttpContext ctx,
                    GiveFurnitureRequest body,
                    DashboardOperationsService ops,
                    CancellationToken ct
                ) =>
                {
                    if (
                        body is null
                        || body.PlayerId <= 0
                        || body.DefinitionId <= 0
                        || !HasReason(body.Reason)
                    )
                        return Results.BadRequest(new { error = "invalid_request" });

                    return Results.Ok(
                        await ops.GiveFurnitureAsync(body, ctx.ActorEmail(), ct)
                            .ConfigureAwait(false)
                    );
                }
            )
            .RequireAuthorization(Capabilities.Dashboard.OpsGrantItem)
            .WithTags(TagOperations);

        app.MapPost(
                "/api/ops/player/kick",
                async (
                    HttpContext ctx,
                    KickPlayerRequest body,
                    DashboardOperationsService ops,
                    CancellationToken ct
                ) =>
                {
                    if (body is null || body.PlayerId <= 0 || !HasReason(body.Reason))
                        return Results.BadRequest(new { error = "invalid_request" });

                    return Results.Ok(
                        await ops.KickPlayerAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                    );
                }
            )
            .RequireAuthorization(Capabilities.Dashboard.OpsKickPlayer)
            .WithTags(TagOperations);
    }

    /// <summary>Serves the bundled SPA shell and hashed assets. Registered only when the front-end is enabled.</summary>
    public static void MapFrontend(WebApplication app)
    {
        app.MapGet(
                "/assets/{file}",
                (string file, DashboardAssetStore store) =>
                    store.TryGetAsset(file, out var bytes, out var contentType)
                        ? Results.Bytes(bytes, contentType)
                        : Results.NotFound()
            )
            .AllowAnonymous()
            .ExcludeFromDescription();

        // SPA client-side routing: any non-API navigation returns the shell; the client gates views
        // from /api/me. Unknown /api routes still 404 as JSON.
        app.MapFallback(
                (HttpContext ctx, DashboardAssetStore store) =>
                    (ctx.Request.Path.Value ?? "/").StartsWith(
                        "/api/",
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? Results.Json(
                            new { error = "not_found" },
                            statusCode: StatusCodes.Status404NotFound
                        )
                        : Results.Bytes(store.GetHtmlBytes("index.html"), store.HtmlContentType)
            )
            .AllowAnonymous()
            .ExcludeFromDescription();
    }

    private static IResult IssueSession(HttpContext ctx, DashboardLoginResult result)
    {
        var sessions = ctx.RequestServices.GetRequiredService<DashboardSessionStore>();
        ctx.Response.Cookies.Append(
            DashboardAuthenticationHandler.SessionCookieName,
            result.SessionId!,
            new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                IsEssential = true,
                MaxAge = TimeSpan.FromSeconds(sessions.LifetimeSeconds),
            }
        );

        return Results.Ok(BuildIdentity(result.Principal!));
    }

    private static object BuildIdentity(DashboardPrincipal principal) =>
        new
        {
            email = principal.Email,
            superuser = principal.Permissions.IsSuperUser,
            capabilities = principal.Permissions.Capabilities,
        };

    private static async Task<IResult> Ok(Task<object> task) =>
        Results.Ok(await task.ConfigureAwait(false));

    private static async Task<IResult> OkNullable(Task<object?> task)
    {
        var payload = await task.ConfigureAwait(false);
        return payload is null
            ? Results.Json(new { error = "not_found" }, statusCode: StatusCodes.Status404NotFound)
            : Results.Ok(payload);
    }

    private static bool HasReason(string? reason) =>
        !string.IsNullOrWhiteSpace(reason) && reason.Trim().Length >= 3;
}
