using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Primitives.Navigator;
using Vortex.Primitives.Plugins;

namespace Vortex.Navigator;

public sealed class NavigatorModule : IHostPluginModule
{
    public string Key => "turbo-navigator";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.AddSingleton<INavigatorService, NavigatorService>();
        services.AddSingleton<INavigatorProvider, NavigatorProvider>();
    }
}
