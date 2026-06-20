using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Turbo.WebApi.Configuration;
using Turbo.WebApi.Services;
using Turbo.WebApi.Session;

namespace Turbo.WebApi.Hosting;

/// <summary>
/// Hosts the client-facing web API as a self-contained ASP.NET Core (Kestrel) application running
/// inside the Orleans generic host — exactly like <c>Turbo.Dashboard.API</c>'s web host. It is its own
/// isolated minimal web app listening on the configured host/port, but reuses the singletons
/// registered by <see cref="WebApiModule"/> in the parent container by forwarding them into the web
/// app's DI. The HTTP surface is the minimal-API endpoints in <see cref="WebApiEndpoints"/>, documented
/// with Swagger/OpenAPI and hardened with CORS, rate limiting and optional HTTPS/HSTS. Disabled unless
/// <see cref="WebApiConfig.Enabled"/> is set.
/// </summary>
internal sealed class WebApiWebHost(
    IServiceProvider rootServices,
    IOptions<WebApiConfig> options,
    ILogger<WebApiWebHost> logger
) : BackgroundService
{
    private readonly WebApiConfig _config = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Enabled)
        {
            return;
        }

        string httpPrefix = $"http://{_config.Host}:{_config.Port}";
        string prefixes = _config.HttpsEnabled
            ? $"{httpPrefix}, https://{_config.Host}:{_config.HttpsPort}"
            : httpPrefix;

        WebApplication app;

        try
        {
            app = BuildApp();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to build Turbo web API");
            return;
        }

        try
        {
            await app.StartAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start Turbo web API on {Prefixes}", prefixes);
            await app.DisposeAsync().ConfigureAwait(false);
            return;
        }

        logger.LogInformation(
            "Turbo web API listening on {Prefixes} (Swagger UI at {HttpPrefix}/swagger)",
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

    private WebApplication BuildApp()
    {
        string httpPrefix = $"http://{_config.Host}:{_config.Port}";
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();

        builder.WebHost.UseUrls(
            _config.HttpsEnabled
                ? new[] { httpPrefix, $"https://{_config.Host}:{_config.HttpsPort}" }
                : new[] { httpPrefix }
        );

        ConfigureKestrel(builder);

        builder.Logging.ClearProviders();

        ForwardSingletons(builder.Services);
        WebApiAppConfigurator.ConfigureServices(builder.Services, _config);

        WebApplication app = builder.Build();

        WebApiAppConfigurator.ConfigurePipeline(app, _config);
        WebApiEndpoints.Map(app);

        return app;
    }

    private void ConfigureKestrel(WebApplicationBuilder builder)
    {
        if (!_config.HttpsEnabled)
        {
            return;
        }

        if (
            string.IsNullOrWhiteSpace(_config.CertificatePath)
            || string.IsNullOrWhiteSpace(_config.CertificatePassword)
        )
        {
            return;
        }

        builder.WebHost.ConfigureKestrel(kestrel =>
            kestrel.ConfigureHttpsDefaults(https =>
                https.ServerCertificate = X509CertificateLoader.LoadPkcs12FromFile(
                    _config.CertificatePath,
                    _config.CertificatePassword
                )
            )
        );
    }

    /// <summary>
    /// Shares the web API singletons constructed in the parent container with the web app's DI, so
    /// endpoints resolve the very same instances (already wired to their EF dependencies) instead of
    /// rebuilding the object graph.
    /// </summary>
    private void ForwardSingletons(IServiceCollection services)
    {
        services.AddSingleton(rootServices.GetRequiredService<WebApiSessionStore>());
        services.AddSingleton(rootServices.GetRequiredService<IWebApiAuthService>());
        services.AddSingleton(rootServices.GetRequiredService<IWebApiPlayerService>());
        services.AddSingleton(options);
    }
}
