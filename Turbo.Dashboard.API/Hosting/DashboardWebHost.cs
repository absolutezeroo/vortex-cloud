using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Turbo.Dashboard.API.Api;
using Turbo.Dashboard.API.Http;
using Turbo.Dashboard.API.Infrastructure;
using Turbo.Dashboard.API.Operations;
using Turbo.Dashboard.API.Security;
using Turbo.Observability.Configuration;
using Turbo.Observability.Diagnostics;
using Turbo.Primitives.Observability;
using Turbo.Primitives.Permissions;

namespace Turbo.Dashboard.API.Hosting;

/// <summary>
///     Hosts the admin dashboard API as a self-contained ASP.NET Core (Kestrel) application running inside
///     the Orleans generic host. It is intentionally isolated — its own minimal web app, listening on the
///     configured dashboard host/port — but reuses the singletons registered by <c>DashboardApiModule</c>
///     in the parent container by forwarding them into the web app's DI. The HTTP surface is a set of
///     minimal-API endpoints (see <see cref="DashboardEndpoints" />) documented with Swagger/OpenAPI, which
///     gives operators interactive docs and provides the foundation for a future public API. Disabled
///     unless <see cref="ObservabilityConfig.DashboardEnabled" /> is set; the SPA front-end is served only
///     when <see cref="ObservabilityConfig.DashboardFrontendEnabled" /> is also set.
/// </summary>
internal sealed class DashboardWebHost(
    IServiceProvider rootServices,
    IOptions<ObservabilityConfig> options,
    ILogger<DashboardWebHost> logger
) : BackgroundService
{
    // Capability policies are named after the capability string they require.
    private static readonly string[] DashboardCapabilities =
    [
        Capabilities.Dashboard.OverviewRead,
        Capabilities.Dashboard.AuditRead,
        Capabilities.Dashboard.EconomyRead,
        Capabilities.Dashboard.PlayersRead,
        Capabilities.Dashboard.FurnitureRead,
        Capabilities.Dashboard.OpsGrantCurrency,
        Capabilities.Dashboard.OpsGrantItem,
        Capabilities.Dashboard.OpsKickPlayer,
        Capabilities.Dashboard.OpsManageVouchers,
        Capabilities.Dashboard.OpsBanAccount,
        Capabilities.Dashboard.OpsMutePlayer,
        Capabilities.Dashboard.OpsTradingLock,
        Capabilities.Dashboard.OpsCfhManage,
        Capabilities.Dashboard.OpsRoomsManage,
        Capabilities.Dashboard.CatalogRead,
        Capabilities.Dashboard.OpsCatalogManage,
        Capabilities.Dashboard.OpsFurnitureManage,
    ];

    private readonly ObservabilityConfig _config = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.DashboardEnabled)
        {
            return;
        }

        DateTime startedAtUtc = DateTime.UtcNow;
        string prefix = $"http://{_config.DashboardHost}:{_config.DashboardPort}";

        WebApplication app;

        try
        {
            app = BuildApp(prefix, () => startedAtUtc);
        }
        catch (Exception ex)
        {
            logger.LogError(
                TurboEventIds.DashboardFault,
                ex,
                "Failed to build Turbo dashboard API"
            );
            return;
        }

        try
        {
            await app.StartAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(
                TurboEventIds.DashboardFault,
                ex,
                "Failed to start Turbo dashboard API on {Prefix}",
                prefix
            );
            await app.DisposeAsync().ConfigureAwait(false);
            return;
        }

        logger.LogInformation(
            TurboEventIds.DashboardReady,
            "Turbo dashboard API listening on {Prefix} (Swagger UI at {Prefix}/swagger)",
            prefix,
            prefix
        );

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Host shutting down.
        }

        await app.StopAsync(CancellationToken.None).ConfigureAwait(false);
        await app.DisposeAsync().ConfigureAwait(false);
    }

    private WebApplication BuildApp(string prefix, Func<DateTime> startedAtUtc)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(prefix);

        // The dashboard web app keeps its own logging quiet; lifecycle messages come from the parent
        // logger above so they sit alongside the rest of the emulator output.
        builder.Logging.ClearProviders();

        ForwardSingletons(builder.Services);
        ConfigureAuth(builder.Services);
        ConfigureSwagger(builder.Services);

        WebApplication app = builder.Build();

        ConfigurePipeline(app);

        DashboardEndpoints.MapAuth(app);
        DashboardEndpoints.MapReadApi(app, startedAtUtc);
        DashboardEndpoints.MapOperations(app);
        DashboardEndpoints.MapMeta(app);

        if (_config.DashboardFrontendEnabled)
        {
            DashboardEndpoints.MapFrontend(app);
        }

        return app;
    }

    /// <summary>
    ///     Shares the dashboard singletons constructed in the parent container with the web app's DI, so
    ///     endpoints resolve the very same instances (which already have their Orleans/EF dependencies
    ///     satisfied) rather than rebuilding the object graph.
    /// </summary>
    private void ForwardSingletons(IServiceCollection services)
    {
        services.AddSingleton(rootServices.GetRequiredService<DashboardApiService>());
        services.AddSingleton(rootServices.GetRequiredService<DashboardOperationsService>());
        services.AddSingleton(rootServices.GetRequiredService<DashboardAuthService>());
        services.AddSingleton(rootServices.GetRequiredService<DashboardSessionStore>());
        services.AddSingleton(rootServices.GetRequiredService<DashboardAssetStore>());
        services.AddSingleton(rootServices.GetRequiredService<DashboardAuditEmitter>());
        services.AddSingleton(options);
    }

    private static void ConfigureAuth(IServiceCollection services)
    {
        // Our cookie/session scheme is the default, so the default authorization policy
        // (RequireAuthenticatedUser) already gates plain [Authorize]/RequireAuthorization() endpoints.
        services
            .AddAuthentication(DashboardAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, DashboardAuthenticationHandler>(
                DashboardAuthenticationHandler.SchemeName,
                null
            );

        services.AddAuthorization(authorization =>
        {
            // One policy per capability; a wildcard ("*") capability satisfies every policy.
            foreach (string capability in DashboardCapabilities)
            {
                authorization.AddPolicy(
                    capability,
                    policy =>
                        policy
                            .RequireAuthenticatedUser()
                            .RequireClaim(
                                DashboardAuthenticationHandler.CapabilityClaimType,
                                capability,
                                Capabilities.Wildcard
                            )
                );
            }
        });
    }

    private static void ConfigureSwagger(IServiceCollection services)
    {
        // CreateSlimBuilder() trims the regex route constraint, but Swashbuckle's Swagger middleware
        // declares its document route with one — re-register it so the middleware can be built.
        services.Configure<RouteOptions>(routing =>
            routing.SetParameterPolicy<RegexInlineRouteConstraint>("regex")
        );

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(swagger =>
        {
            swagger.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "Turbo Dashboard API",
                    Version = "v1",
                    Description =
                        "Operations and observability API for the Turbo emulator. Authenticate via "
                        + "POST /api/login (issues the dash_session cookie); endpoints are authorized "
                        + "by dashboard.* capabilities.",
                }
            );

            swagger.AddSecurityDefinition(
                DashboardAuthenticationHandler.SchemeName,
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Cookie,
                    Name = DashboardAuthenticationHandler.SessionCookieName,
                    Description =
                        "Session cookie issued by POST /api/login. Sent automatically by the browser "
                        + "on same-origin requests.",
                }
            );

            swagger.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference(
                        DashboardAuthenticationHandler.SchemeName,
                        document
                    ),
                    new List<string>()
                },
            });
        });
    }

    private void ConfigurePipeline(WebApplication app)
    {
        string csp = DashboardSecurityHeaders.BuildCsp(
            _config.FurniIconUrlTemplate,
            _config.CatalogIconUrlTemplate
        );
        DashboardAuditEmitter emitter = app.Services.GetRequiredService<DashboardAuditEmitter>();

        // Hardened headers for the SPA + JSON API. Swagger UI ships an inline bootstrap script, so it
        // is exempt from the strict CSP (it is operator-only and same-origin).
        app.Use(
            async (ctx, next) =>
            {
                if (
                    !(ctx.Request.Path.Value ?? "/").StartsWith(
                        "/swagger",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    DashboardSecurityHeaders.Apply(ctx.Response, csp);
                }

                await next().ConfigureAwait(false);
            }
        );

        app.UseSwagger();
        app.UseSwaggerUI(ui =>
        {
            ui.SwaggerEndpoint("/swagger/v1/swagger.json", "Turbo Dashboard API v1");
            ui.DocumentTitle = "Turbo Dashboard API";
        });

        app.UseAuthentication();
        app.UseAuthorization();

        // HTTP access audit trail. Login/logout audit themselves; operation success is audited by
        // DashboardOperationsService (with correlation id), so for operation routes only failures are
        // logged here to avoid duplicate records.
        app.Use(
            async (ctx, next) =>
            {
                await next().ConfigureAwait(false);

                string path = ctx.Request.Path.Value ?? "/";

                if (!path.StartsWith("/api/", StringComparison.Ordinal))
                {
                    return;
                }

                if (path is "/api/login" or "/api/logout")
                {
                    return;
                }

                int status = ctx.Response.StatusCode;
                bool isOperation =
                    path.StartsWith("/api/ops/", StringComparison.Ordinal)
                    || path.StartsWith("/api/v1/operations/", StringComparison.Ordinal);

                if (isOperation && status == StatusCodes.Status200OK)
                {
                    return;
                }

                AuditResult result = status switch
                {
                    StatusCodes.Status200OK => AuditResult.Success,
                    StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden =>
                        AuditResult.Denied,
                    _ => AuditResult.Failed,
                };

                emitter.Emit(path, result, status, "HttpAccess", ctx.ActorEmail());
            }
        );
    }
}
