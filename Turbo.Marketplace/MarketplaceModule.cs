using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Turbo.Contracts.Plugins;

namespace Turbo.Marketplace;

public sealed class MarketplaceModule : IHostPluginModule
{
    public string Key => "turbo-marketplace";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
    }
}
