using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vortex.Primitives.Hosting;
using Vortex.Primitives.Plugins;
using Vortex.WebApi.Configuration;
using Vortex.WebApi.Hosting;
using Vortex.WebApi.Services;
using Vortex.WebApi.Session;

namespace Vortex.WebApi;

/// <summary>
/// Registers the client-facing web API: the cookie session store, the auth/player services and the
/// self-hosted ASP.NET Core (Kestrel) front controller. The HTTP app is isolated in
/// <see cref="WebApiWebHost"/>, exactly like <c>Vortex.Dashboard.API</c>.
/// </summary>
public sealed class WebApiModule : IHostPluginModule
{
    public string Key => "turbo-webapi";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        // Validated at start so an insecure listener (cleartext off-box) or an unloadable HTTPS
        // certificate is refused before the socket opens, not discovered in production.
        services
            .AddOptions<WebApiConfig>()
            .Bind(builder.Configuration.GetSection(WebApiConfig.SECTION_NAME))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<WebApiConfig>, WebApiConfigValidator>();

        // Shared with DashboardApiModule; whichever module registers first wins.
        services.TryAddSingleton<RequiredServiceGuard>();

        services.TryAddSingleton<WebApiSessionStore>();
        services.TryAddSingleton<IWebApiAuthService, WebApiAuthService>();
        services.TryAddSingleton<IWebApiPlayerService, WebApiPlayerService>();

        services.AddHostedService<WebApiWebHost>();
    }
}
