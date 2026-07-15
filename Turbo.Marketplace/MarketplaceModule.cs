using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Turbo.Marketplace.Providers;
using Turbo.Primitives.Marketplace.Providers;
using Turbo.Primitives.Plugins;

namespace Turbo.Marketplace;

public sealed class MarketplaceModule : IHostPluginModule
{
    public string Key => "turbo-marketplace";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.AddSingleton<IMarketplaceSettingsProvider, MarketplaceSettingsProvider>();
    }
}
