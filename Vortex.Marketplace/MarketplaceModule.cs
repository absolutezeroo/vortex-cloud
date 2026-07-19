using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Marketplace.Providers;
using Vortex.Primitives.Marketplace.Providers;
using Vortex.Primitives.Plugins;

namespace Vortex.Marketplace;

public sealed class MarketplaceModule : IHostPluginModule
{
    public string Key => "turbo-marketplace";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.AddSingleton<IMarketplaceSettingsProvider, MarketplaceSettingsProvider>();
    }
}
