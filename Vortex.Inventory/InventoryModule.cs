using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Inventory.Configuration;
using Vortex.Inventory.Factories;
using Vortex.Primitives.Inventory.Factories;
using Vortex.Primitives.Plugins;

namespace Vortex.Inventory;

public sealed class InventoryModule : IHostPluginModule
{
    public string Key => "turbo-inventory";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.Configure<InventoryConfig>(
            builder.Configuration.GetSection(InventoryConfig.SECTION_NAME)
        );

        services.AddSingleton<IInventoryFurnitureLoader, InventoryFurnitureLoader>();
    }
}
