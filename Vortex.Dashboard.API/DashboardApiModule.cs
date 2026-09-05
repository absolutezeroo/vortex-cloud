using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Hosting;
using Vortex.Dashboard.API.Http;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Dashboard.API.Operations;
using Vortex.Dashboard.API.Security;
using Vortex.Observability.Configuration;
using Vortex.Primitives.Authentication;
using Vortex.Primitives.Hosting;
using Vortex.Primitives.Plugins;

namespace Vortex.Dashboard.API;

/// <summary>
/// Registers the independent admin dashboard API: authentication/session services, the asset store,
/// the read JSON API, the operations layer and the self-hosted HTTP front controller. It consumes
/// Observability (metrics, audit sinks, health/incidents) and Database but owns no observability
/// pipeline of its own — the audit and error-grouping writers live in <c>ObservabilityModule</c> so
/// they run regardless of whether the dashboard is enabled.
/// </summary>
public sealed class DashboardApiModule : IHostPluginModule
{
    public string Key => "turbo-dashboard-api";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        // ObservabilityModule binds the same section; both register the validator through
        // TryAddEnumerable so it runs exactly once regardless of module order.
        services
            .AddOptions<ObservabilityConfig>()
            .Bind(builder.Configuration.GetSection(ObservabilityConfig.SECTION_NAME))
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<ObservabilityConfig>,
                ObservabilityConfigValidator
            >()
        );

        // Shared with WebApiModule; whichever module registers first wins.
        services.TryAddSingleton<RequiredServiceGuard>();

        services.TryAddSingleton<DashboardSessionStore>();
        // Registered as the shared interface too, so a password change reaches these sessions
        // without the password service knowing how many session stores exist.
        services.AddSingleton<IAccountSessionRevoker>(sp =>
            sp.GetRequiredService<DashboardSessionStore>()
        );
        services.TryAddSingleton<DashboardAuthService>();
        services.TryAddSingleton<DashboardAssetStore>();
        services.TryAddSingleton<DashboardAssetUrls>();
        // Singleton because it caches a 38 MB parsed furnidata; a per-request instance would parse it
        // again on every keystroke of a search.
        services.TryAddSingleton<GamedataDocumentStore>();
        // Reads the Habbicon spritesheet's metadata off the same asset pack, and caches it until the
        // operator swaps the pack.
        services.TryAddSingleton<HabbiconArtwork>();
        services.TryAddSingleton<DashboardAuditEmitter>();
        services.TryAddSingleton<DashboardApiService>();
        services.TryAddSingleton<DashboardMonitoringReads>();
        services.TryAddSingleton<DashboardOperationsService>();

        // The dashboard runs as a self-contained ASP.NET Core (Kestrel) app inside the generic host.
        services.AddHostedService<DashboardWebHost>();
    }
}
