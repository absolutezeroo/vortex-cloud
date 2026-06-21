using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
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
    private const string ApiV1 = "/api/v1";
    private const string ApiMonitoring = ApiV1 + "/monitoring";
    private const string ApiForensics = ApiV1 + "/forensics";
    private const string ApiEconomy = ApiV1 + "/economy";
    private const string ApiDirectory = ApiV1 + "/directory";
    private const string ApiOperations = ApiV1 + "/operations";
    private const string ApiMeta = ApiV1 + "/meta";

    private sealed record ApiRouteDescriptor(
        string Domain,
        string Path,
        string[] Methods,
        string[] Tags,
        string[] Capabilities,
        bool RequiresAuth,
        bool IsLegacy,
        string? DisplayName
    );

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
                    {
                        return Results.BadRequest(new { error = "invalid_request" });
                    }

                    DashboardLoginResult result = await auth.LoginAsync(
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
                    DashboardPrincipal? principal = ctx.GetDashboardPrincipal();
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
        MapReadGet(
            app,
            ApiMonitoring + "/overview",
            "/api/overview",
            (DashboardApiService api, CancellationToken ct) =>
                Ok(api.OverviewAsync(startedAtUtc(), ct)),
            Capabilities.Dashboard.OverviewRead,
            TagMonitoring
        );

        MapReadGet(
            app,
            ApiMonitoring + "/infrastructure",
            "/api/infrastructure",
            async (DashboardApiService api, CancellationToken ct) =>
                Results.Ok(await api.InfrastructureAsync(ct).ConfigureAwait(false)),
            Capabilities.Dashboard.OverviewRead,
            TagMonitoring
        );

        MapReadGet(
            app,
            ApiMonitoring + "/incidents",
            "/api/incidents",
            async (DashboardApiService api, CancellationToken ct) =>
                Results.Ok(await api.IncidentsAsync(ct).ConfigureAwait(false)),
            Capabilities.Dashboard.OverviewRead,
            TagMonitoring
        );

        MapReadGet(
            app,
            ApiMonitoring + "/packet-stats",
            "/api/packet-stats",
            (DashboardApiService api, CancellationToken ct) => Ok(api.PacketStatsAsync(ct)),
            Capabilities.Dashboard.OverviewRead,
            TagMonitoring
        );

        MapReadGet(
            app,
            ApiForensics + "/audit",
            "/api/audit",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                Ok(api.AuditAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.AuditRead,
            TagForensics
        );

        MapReadGet(
            app,
            ApiForensics + "/moderation/stats",
            "/api/moderation-stats",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                Ok(api.ModerationStatsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.AuditRead,
            TagForensics
        );

        MapReadGet(
            app,
            ApiEconomy + "/ledger",
            "/api/economy",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                Ok(api.EconomyAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.EconomyRead,
            TagEconomy
        );

        MapReadGet(
            app,
            ApiEconomy + "/subscriptions",
            "/api/economy/subscriptions",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                Ok(api.ClubSubscriptionsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.EconomyRead,
            TagEconomy
        );

        app.MapGet(
                ApiV1 + "/rentable-spaces/activity",
                (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                    Ok(api.RentableSpacesAsync(ctx.QueryAsNameValues(), ct))
            )
            .RequireAuthorization(Capabilities.Dashboard.EconomyRead)
            .WithTags(TagEconomy);

        MapReadGet(
            app,
            ApiDirectory + "/search",
            "/api/search",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                Ok(api.SearchAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.AuditRead,
            TagForensics
        );

        MapReadGet(
            app,
            ApiDirectory + "/players",
            "/api/players",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                Ok(api.PlayersAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.PlayersRead,
            TagDirectory
        );

        MapReadGet(
            app,
            ApiDirectory + "/furniture",
            "/api/furniture",
            (HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                Ok(api.FurnitureDefinitionsAsync(ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.FurnitureRead,
            TagDirectory
        );

        MapReadGet(
            app,
            ApiDirectory + "/entity/{id}",
            "/api/item/{id}",
            (string id, HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkNullable(api.ItemAsync(id, ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.AuditRead,
            TagForensics
        );

        MapReadGet(
            app,
            ApiDirectory + "/rooms/{roomId:int}",
            "/api/room/{roomId:int}",
            (int roomId, HttpContext ctx, DashboardApiService api, CancellationToken ct) =>
                OkNullable(api.RoomTimelineAsync(roomId, ctx.QueryAsNameValues(), ct)),
            Capabilities.Dashboard.AuditRead,
            TagForensics
        );
    }

    public static void MapOperations(WebApplication app)
    {
        MapPost(
            app,
            ApiOperations + "/currency/credits",
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
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.GiveCreditsAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsGrantCurrency,
            TagOperations
        );

        MapPost(
            app,
            ApiOperations + "/currency/activity-points",
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
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.GiveActivityPointsAsync(body, ctx.ActorEmail(), ct)
                        .ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsGrantCurrency,
            TagOperations
        );

        MapPost(
            app,
            ApiOperations + "/items/grant",
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
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.GiveFurnitureAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsGrantItem,
            TagOperations
        );

        MapPost(
            app,
            ApiOperations + "/players/kick",
            "/api/ops/player/kick",
            async (
                HttpContext ctx,
                KickPlayerRequest body,
                DashboardOperationsService ops,
                CancellationToken ct
            ) =>
            {
                if (body is null || body.PlayerId <= 0 || !HasReason(body.Reason))
                {
                    return Results.BadRequest(new { error = "invalid_request" });
                }

                return Results.Ok(
                    await ops.KickPlayerAsync(body, ctx.ActorEmail(), ct).ConfigureAwait(false)
                );
            },
            Capabilities.Dashboard.OpsKickPlayer,
            TagOperations
        );
    }

    public static void MapMeta(WebApplication app)
    {
        app.MapGet(
                ApiMeta + "/endpoints",
                (IEnumerable<EndpointDataSource> dataSources) =>
                {
                    ApiRouteDescriptor?[] routes = dataSources
                        .SelectMany(source => source.Endpoints.OfType<RouteEndpoint>())
                        .Select(endpoint =>
                        {
                            string? route = endpoint.RoutePattern.RawText;

                            if (string.IsNullOrWhiteSpace(route) || !route.StartsWith("/api/"))
                            {
                                return (ApiRouteDescriptor?)null;
                            }

                            string domain = ResolveApiDomain(route);

                            string[] methods = endpoint
                                .Metadata.OfType<IHttpMethodMetadata>()
                                .SelectMany(m => m.HttpMethods)
                                .Distinct()
                                .OrderBy(m => m)
                                .ToArray();

                            if (methods.Length == 0)
                            {
                                methods = ["GET"];
                            }

                            string?[] capabilities = endpoint
                                .Metadata.OfType<IAuthorizeData>()
                                .Select(auth => auth.Policy)
                                .Where(policy => !string.IsNullOrWhiteSpace(policy))
                                .Distinct()
                                .OrderBy(policy => policy)
                                .ToArray();

                            string[] tags = endpoint
                                .Metadata.OfType<ITagsMetadata>()
                                .SelectMany(tag => tag.Tags)
                                .Distinct()
                                .OrderBy(tag => tag)
                                .ToArray();

                            return new ApiRouteDescriptor(
                                domain,
                                route,
                                methods,
                                tags,
                                capabilities,
                                capabilities.Length > 0,
                                !route.StartsWith(ApiV1 + "/", StringComparison.OrdinalIgnoreCase),
                                endpoint.DisplayName
                            );
                        })
                        .Where(route => route is not null)
                        .OrderBy(route => route!.Domain)
                        .ThenBy(route => route!.Path)
                        .ToArray();

                    var groups = routes
                        .GroupBy(route => route.Domain)
                        .OrderBy(group => group.Key)
                        .Select(group => new
                        {
                            domain = group.Key,
                            routeCount = group.Count(),
                            methods = group
                                .SelectMany(route => route.Methods)
                                .Distinct()
                                .OrderBy(method => method)
                                .ToArray(),
                        })
                        .ToArray();

                    var methodUsage = routes
                        .SelectMany(route => route.Methods)
                        .GroupBy(method => method)
                        .OrderBy(group => group.Key)
                        .Select(group => new { method = group.Key, count = group.Count() })
                        .ToArray();

                    return Results.Ok(
                        new
                        {
                            version = "1",
                            generatedAt = DateTime.UtcNow,
                            routes,
                            groups,
                            methodUsage,
                        }
                    );
                }
            )
            .RequireAuthorization(Capabilities.Dashboard.OverviewRead)
            .WithTags(TagMonitoring)
            .WithSummary("List dashboard API routes with methods and capability requirements.")
            .WithName("DashboardApiRouteCatalog");
    }

    /// <summary>Serves the bundled SPA shell and hashed assets. Registered only when the front-end is enabled.</summary>
    public static void MapFrontend(WebApplication app)
    {
        app.MapGet(
                "/assets/{file}",
                (string file, DashboardAssetStore store) =>
                    store.TryGetAsset(file, out byte[] bytes, out string contentType)
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
        DashboardSessionStore sessions =
            ctx.RequestServices.GetRequiredService<DashboardSessionStore>();
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
        object? payload = await task.ConfigureAwait(false);
        return payload is null
            ? Results.Json(new { error = "not_found" }, statusCode: StatusCodes.Status404NotFound)
            : Results.Ok(payload);
    }

    private static bool HasReason(string? reason) =>
        !string.IsNullOrWhiteSpace(reason) && reason.Trim().Length >= 3;

    private static void MapReadGet(
        WebApplication app,
        string v1Path,
        string legacyPath,
        Delegate handler,
        string capability,
        string tag
    )
    {
        app.MapGet(v1Path, handler).RequireAuthorization(capability).WithTags(tag);
        app.MapGet(legacyPath, handler).RequireAuthorization(capability).WithTags(tag);
    }

    private static void MapPost(
        WebApplication app,
        string v1Path,
        string legacyPath,
        Delegate handler,
        string capability,
        string tag
    )
    {
        app.MapPost(v1Path, handler).RequireAuthorization(capability).WithTags(tag);
        app.MapPost(legacyPath, handler).RequireAuthorization(capability).WithTags(tag);
    }

    private static string ResolveApiDomain(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return "misc";
        }

        string normalized = route.Trim('/');
        string[] parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 3 && parts[0].Equals("api", StringComparison.OrdinalIgnoreCase))
        {
            if (parts[1].Equals("v1", StringComparison.OrdinalIgnoreCase))
            {
                return parts.Length >= 3 ? parts[2] : "v1";
            }

            return "legacy";
        }

        return "misc";
    }
}
