using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Turbo.Inventory.Configuration;
using Turbo.Inventory.Factories;
using Turbo.Primitives.Inventory.Factories;
using Turbo.Primitives.Plugins;

namespace Turbo.Inventory;

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
