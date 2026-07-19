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
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Dashboard.API.Operations;
using Vortex.Dashboard.API.Security;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

/// <summary>
/// Maps the dashboard HTTP surface onto minimal-API endpoints. Authentication is handled by
/// <see cref="DashboardAuthenticationHandler"/> and authorization by per-capability policies (named
/// after the capability string), so each endpoint just declares the capability it needs via
/// <c>RequireAuthorization</c>. Every endpoint is tagged for Swagger grouping.
/// </summary>
internal static partial class DashboardEndpoints
{
    public const string LoginRateLimitPolicy = "dashboard-login";

    private const string TagAuth = "Auth";
    private const string TagMonitoring = "Monitoring";
    private const string TagForensics = "Forensics";
    private const string TagEconomy = "Economy";
    private const string TagDirectory = "Directory";
    private const string TagOperations = "Operations";
    private const string TagCatalog = "Catalog";
    private const string TagFurniture = "Furniture";
    private const string ApiV1 = "/api/v1";
    private const string ApiMonitoring = ApiV1 + "/monitoring";
    private const string ApiForensics = ApiV1 + "/forensics";
    private const string ApiEconomy = ApiV1 + "/economy";
    private const string ApiDirectory = ApiV1 + "/directory";
    private const string ApiOperations = ApiV1 + "/operations";
    private const string ApiCatalog = ApiV1 + "/catalog";
    private const string ApiFurniture = ApiV1 + "/furniture";
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
            .RequireRateLimiting(LoginRateLimitPolicy)
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
        MapMonitoringReads(app, startedAtUtc);
        MapAuditReads(app);
        MapEconomyReads(app);
        MapDirectoryReads(app);
        MapRoomReads(app);
        MapCatalogReads(app);
        MapFurnitureReads(app);
        MapStatsReads(app);
        MapTargetedOfferReads(app);
    }

    public static void MapOperations(WebApplication app)
    {
        MapCurrencyOperations(app);
        MapVoucherOperations(app);
        MapModerationOperations(app);
        MapRoomOperations(app);
        MapCatalogOperations(app);
        MapFurnitureOperations(app);
        MapTargetedOfferOperations(app);
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
                Secure = ctx.Request.IsHttps,
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

    // VSTHRD003: both helpers deliberately await a task handed in by the endpoint delegate (already
    // started by the DashboardApiService call at the call site) purely to wrap its result/null-check
    // in a Results.Ok/Json — there is no deadlock risk since nothing here owns or blocks on the task.
#pragma warning disable VSTHRD003
    private static async Task<IResult> OkAsync(Task<object> task) =>
        Results.Ok(await task.ConfigureAwait(false));

    private static async Task<IResult> OkNullableAsync(Task<object?> task)
    {
        object? payload = await task.ConfigureAwait(false);
        return payload is null
            ? Results.Json(new { error = "not_found" }, statusCode: StatusCodes.Status404NotFound)
            : Results.Ok(payload);
    }
#pragma warning restore VSTHRD003

    private static bool HasReason(string? reason) =>
        !string.IsNullOrWhiteSpace(reason) && reason.Trim().Length >= 3;

    /// <summary>A permanent sanction needs no duration; a temporary one needs a positive duration.</summary>
    private static bool HasValidDuration(bool permanent, int? durationSeconds) =>
        permanent || durationSeconds is > 0;

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
