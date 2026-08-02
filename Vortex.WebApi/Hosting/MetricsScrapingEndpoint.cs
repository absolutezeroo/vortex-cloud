using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using Vortex.Primitives.Observability;
using Vortex.WebApi.Configuration;

namespace Vortex.WebApi.Hosting;

/// <summary>
/// The Prometheus scraping endpoint, served on the web API listener next to <c>/health</c>.
/// </summary>
/// <remarks>
/// <para>
/// OpenTelemetry only subscribes here — it does not produce anything. Both meters it reads are
/// process-global <c>System.Diagnostics.Metrics</c> meters: Vortex's own instruments, and the
/// <c>Microsoft.Orleans</c> counters the framework publishes on its own (grain activations, message
/// queues, scheduler latency). That is also why hosting the reader inside the web API's isolated
/// container works at all — a <c>MeterProvider</c> listens to the process, not to its container.
/// </para>
/// <para>
/// <b>Access control.</b> The endpoint inherits every listener-level control <c>/health</c> has: the
/// whole web API is off by default, binds to localhost by default, and <c>ListenerSecurity</c>
/// refuses a cleartext bind to a non-local address unless that is explicitly opted into — plus the
/// shared security headers and the CORS allow-list. Two things are deliberately stricter, because a
/// scrape is not a liveness probe: it is off even when the API is on, and it answers loopback
/// callers only until a bearer token is configured. <c>/health</c> discloses three booleans; a scrape
/// discloses the live population, the active room count, per-step room-tick timings and packet
/// volumes, which is exactly the reconnaissance an attacker would want and exactly what a
/// bearer-token-authenticated scraper is the conventional answer to.
/// </para>
/// </remarks>
internal static class MetricsScrapingEndpoint
{
    private const string BearerPrefix = "Bearer ";

    public static void ConfigureServices(IServiceCollection services, WebApiConfig config)
    {
        if (!config.MetricsEnabled)
        {
            return;
        }

        services
            .AddOpenTelemetry()
            .WithMetrics(metrics =>
                metrics
                    .AddMeter(VortexMeterNames.VORTEX)
                    .AddMeter(VortexMeterNames.ORLEANS)
                    .AddPrometheusExporter()
            );
    }

    public static void ConfigurePipeline(WebApplication app, WebApiConfig config)
    {
        if (!config.MetricsEnabled)
        {
            return;
        }

        // Guarding ahead of the exporter's own middleware rather than inside its branch keeps the
        // decision in plain sight and means an unauthorized request is rejected before any metric is
        // collected or rendered.
        app.Use(
            async (ctx, next) =>
            {
                if (!ctx.Request.Path.StartsWithSegments(config.MetricsPath))
                {
                    await next().ConfigureAwait(false);
                    return;
                }

                if (config.MetricsToken.Length == 0)
                {
                    if (!IsLoopback(ctx))
                    {
                        // No token configured means "local scraper only". 404, not 403: an off-box
                        // caller learns nothing about whether the endpoint exists.
                        ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }
                }
                else if (!HasValidToken(ctx, config.MetricsToken))
                {
                    // A token IS configured, so the operator knows the endpoint is there; answer the
                    // honest 401 so a misconfigured scraper is debuggable.
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    ctx.Response.Headers.WWWAuthenticate = "Bearer";
                    return;
                }

                await next().ConfigureAwait(false);
            }
        );

        app.UseOpenTelemetryPrometheusScrapingEndpoint(config.MetricsPath);
    }

    /// <summary>
    /// Loopback as ASP.NET Core sees it — which is the forwarded client address when, and only when,
    /// a trusted proxy has been declared (see <see cref="WebApiConfig.UseForwardedHeaders"/>).
    /// </summary>
    private static bool IsLoopback(HttpContext ctx)
    {
        IPAddress? remote = ctx.Connection.RemoteIpAddress;

        return remote is not null && IPAddress.IsLoopback(remote);
    }

    private static bool HasValidToken(HttpContext ctx, string expected)
    {
        string authorization = ctx.Request.Headers.Authorization.ToString();

        if (!authorization.StartsWith(BearerPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        // Fixed-time: a length-independent comparison would leak the token a character at a time to
        // anyone willing to scrape timings.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(authorization[BearerPrefix.Length..]),
            Encoding.UTF8.GetBytes(expected)
        );
    }
}
