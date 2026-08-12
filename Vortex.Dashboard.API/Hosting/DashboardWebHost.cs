using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Http;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Dashboard.API.Operations;
using Vortex.Dashboard.API.Security;
using Vortex.Observability.Configuration;
using Vortex.Observability.Diagnostics;
using Vortex.Primitives.Hosting;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Permissions;

namespace Vortex.Dashboard.API.Hosting;

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
    RequiredServiceGuard guard,
    ILogger<DashboardWebHost> logger
) : BackgroundService
{
    internal const string SERVICE_NAME = "Vortex dashboard API";

    internal const string REQUIRED_OPTION_PATH =
        $"{ObservabilityConfig.SECTION_NAME}:{nameof(ObservabilityConfig.DashboardRequired)}";

    internal const string ALLOW_INSECURE_OPTION_PATH =
        $"{ObservabilityConfig.SECTION_NAME}:{nameof(ObservabilityConfig.DashboardAllowInsecureRemoteHttp)}";

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
        Capabilities.Dashboard.GroupsRead,
        Capabilities.Dashboard.PetsRead,
        Capabilities.Dashboard.CfhRead,
        Capabilities.Dashboard.CatalogPurchasesRead,
        Capabilities.Dashboard.WiredRead,
        Capabilities.Dashboard.TargetedOffersRead,
        Capabilities.Dashboard.OpsTargetedOffersManage,
        Capabilities.Dashboard.QuestsRead,
        Capabilities.Dashboard.OpsQuestsManage,
        Capabilities.Dashboard.PollsRead,
        Capabilities.Dashboard.OpsPollsManage,
        Capabilities.Dashboard.PrizePoolsRead,
        Capabilities.Dashboard.OpsPrizePoolsManage,
        Capabilities.Dashboard.MysteryBoxRead,
        Capabilities.Dashboard.OpsMysteryBoxManage,
        Capabilities.Dashboard.ConfigRead,
        Capabilities.Dashboard.OpsConfigManage,
        Capabilities.Dashboard.PerformanceRead,
        Capabilities.Dashboard.AchievementsRead,
        Capabilities.Dashboard.BotsRead,
        Capabilities.Dashboard.NavigatorRead,
        Capabilities.Dashboard.OpsNavigatorManage,
        Capabilities.Dashboard.SocialRead,
        Capabilities.Dashboard.StaffRead,
        Capabilities.Dashboard.CollectiblesRead,
        Capabilities.Dashboard.OpsStaffManage,
        Capabilities.Dashboard.OpsContentManage,
    ];

    private readonly ObservabilityConfig _config = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.DashboardEnabled)
        {
            return;
        }

        DateTime startedAtUtc = DateTime.UtcNow;
        string httpPrefix = $"http://{_config.DashboardHost}:{_config.DashboardPort}";
        string prefixes = _config.DashboardHttpsEnabled
            ? $"{httpPrefix}, https://{_config.DashboardHost}:{_config.DashboardHttpsPort}"
            : httpPrefix;

        // Refuse cleartext off-box exposure before a socket is ever opened: the dashboard session
        // cookie grants dashboard.* capabilities, i.e. full operator access.
        ListenerSecurityResult listener = ListenerSecurity.ValidateListener(
            SERVICE_NAME,
            _config.DashboardHost,
            _config.DashboardPort,
            _config.DashboardHttpsEnabled,
            _config.DashboardAllowInsecureRemoteHttp,
            ALLOW_INSECURE_OPTION_PATH
        );

        if (!listener.IsAllowed)
        {
            Fail(new InvalidOperationException(listener.Message));
            return;
        }

        if (listener.Message is not null)
        {
            logger.LogWarning(VortexEventIds.DashboardFault, "{ListenerWarning}", listener.Message);
        }

        WebApplication app;

        try
        {
            app = BuildApp(httpPrefix, () => startedAtUtc);
        }
        catch (Exception ex)
        {
            Fail(ex);
            return;
        }

        try
        {
            await app.StartAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await app.DisposeAsync().ConfigureAwait(false);
            Fail(ex);
            return;
        }

        guard.ReportStarted(SERVICE_NAME);

        logger.LogInformation(
            VortexEventIds.DashboardReady,
            "Vortex dashboard API listening on {Prefixes} (Swagger UI at {HttpPrefix}/swagger)",
            prefixes,
            httpPrefix
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

    /// <summary>
    /// Routes a build/start failure through the shared policy: required means the host comes down
    /// with a non-zero exit code, optional means the emulator is flagged degraded and carries on.
    /// </summary>
    private void Fail(Exception ex) =>
        guard.ReportStartupFailure(
            SERVICE_NAME,
            _config.DashboardEnabled,
            _config.DashboardRequired,
            ex,
            REQUIRED_OPTION_PATH
        );

    private WebApplication BuildApp(string httpPrefix, Func<DateTime> startedAtUtc)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(
            _config.DashboardHttpsEnabled
                ? [httpPrefix, $"https://{_config.DashboardHost}:{_config.DashboardHttpsPort}"]
                : [httpPrefix]
        );

        ConfigureKestrel(builder);

        // The dashboard web app keeps its own logging quiet; lifecycle messages come from the parent
        // logger above so they sit alongside the rest of the emulator output.
        builder.Logging.ClearProviders();

        ForwardSingletons(builder.Services);
        ConfigureForwardedHeaders(builder.Services);
        ConfigureAuth(builder.Services);
        ConfigureRateLimiting(builder.Services);
        ConfigureHttpsRedirection(builder.Services);
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
        services.AddSingleton(rootServices.GetRequiredService<DashboardAssetUrls>());
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

    private void ConfigureKestrel(WebApplicationBuilder builder)
    {
        if (!_config.DashboardHttpsEnabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_config.DashboardCertificatePath))
        {
            // No explicit certificate: Kestrel falls back to the ASP.NET Core development
            // certificate. HttpsCertificateValidator refuses this outside Development at startup,
            // so reaching here means it is a deliberate local-dev configuration.
            return;
        }

        // Load eagerly and let failures propagate: BuildApp()'s caller routes them through the
        // required/optional policy. Previously a bad path or password silently skipped HTTPS
        // configuration, leaving a listener that looked like HTTPS but was never configured.
        builder.WebHost.ConfigureKestrel(kestrel =>
            kestrel.ConfigureHttpsDefaults(https =>
                https.ServerCertificate = X509CertificateLoader.LoadPkcs12FromFile(
                    _config.DashboardCertificatePath,
                    _config.DashboardCertificatePassword
                )
            )
        );
    }

    /// <summary>
    /// Applies <see cref="ForwardedHeadersOptions"/> only when the operator has explicitly declared a
    /// reverse proxy. ASP.NET Core trusts no proxy by default, and processing
    /// <c>X-Forwarded-For</c>/<c>-Proto</c> from an unknown sender would let any client forge its
    /// source IP (which partitions the login rate limiter) and claim a TLS-terminated request.
    /// </summary>
    private void ConfigureForwardedHeaders(IServiceCollection services)
    {
        if (!_config.DashboardUseForwardedHeaders)
        {
            return;
        }

        services.Configure<ForwardedHeadersOptions>(forwarded =>
        {
            forwarded.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Defaults are loopback-only; clear them so the explicit allow-list below is authoritative.
            forwarded.KnownProxies.Clear();
            forwarded.KnownIPNetworks.Clear();

            foreach (string proxy in _config.DashboardKnownProxies)
            {
                if (IPAddress.TryParse(proxy, out IPAddress? address))
                {
                    forwarded.KnownProxies.Add(address);
                }
            }

            foreach (string network in _config.DashboardKnownNetworks)
            {
                string[] parts = network.Split('/', 2);

                if (
                    parts.Length == 2
                    && IPAddress.TryParse(parts[0], out IPAddress? prefix)
                    && int.TryParse(parts[1], out int length)
                )
                {
                    forwarded.KnownIPNetworks.Add(new System.Net.IPNetwork(prefix, length));
                }
            }
        });
    }

    private void ConfigureRateLimiting(IServiceCollection services)
    {
        ObservabilityConfig.DashboardRateLimitOptions limit = _config.DashboardLoginRateLimit;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(
                DashboardEndpoints.LoginRateLimitPolicy,
                httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = limit.PermitLimit,
                            Window = TimeSpan.FromSeconds(limit.WindowSeconds),
                            QueueLimit = limit.QueueLimit,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            AutoReplenishment = true,
                        }
                    )
            );
        });
    }

    private void ConfigureHttpsRedirection(IServiceCollection services)
    {
        if (!_config.DashboardHttpsEnabled)
        {
            return;
        }

        services.AddHttpsRedirection(https => https.HttpsPort = _config.DashboardHttpsPort);
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
                    Title = "Vortex Dashboard API",
                    Version = "v1",
                    Description =
                        "Operations and observability API for the Vortex emulator. Authenticate via "
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
        if (_config.DashboardUseForwardedHeaders)
        {
            // Must run before anything reads the scheme or the remote IP (HTTPS redirection,
            // rate limiting, auth, audit).
            app.UseForwardedHeaders();
        }

        string csp = DashboardSecurityHeaders.BuildCsp(
            app.Services.GetRequiredService<DashboardAssetUrls>().ImgSrcOrigins
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

        if (_config.DashboardHttpsEnabled)
        {
            if (_config.DashboardHstsEnabled)
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
        }

        app.UseSwagger();
        app.UseSwaggerUI(ui =>
        {
            ui.SwaggerEndpoint("/swagger/v1/swagger.json", "Vortex Dashboard API v1");
            ui.DocumentTitle = "Vortex Dashboard API";
        });

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

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
