using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Turbo.Furniture.Configuration;
using Turbo.Furniture.Providers;
using Turbo.Primitives.Furniture;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Plugins;

namespace Turbo.Furniture;

public sealed class FurnitureModule : IHostPluginModule
{
    public string Key => "turbo-furniture";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.Configure<FurnitureConfig>(
            builder.Configuration.GetSection(FurnitureConfig.SECTION_NAME)
        );

        services.AddSingleton<IFurnitureService, FurnitureService>();
        services.AddSingleton<IFurnitureDefinitionProvider, FurnitureDefinitionProvider>();
        services.AddSingleton<IFurnitureAdminService, FurnitureAdminService>();
        services.AddSingleton<IStuffDataFactory, StuffDataFactory>();
    }
}
