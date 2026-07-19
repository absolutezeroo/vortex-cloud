using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vortex.Catalog.Configuration;
using Vortex.Catalog.Providers;
using Vortex.Database.Context;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Catalog.Enums;
using Vortex.Primitives.Catalog.Providers;
using Vortex.Primitives.Catalog.Tags;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Plugins;

namespace Vortex.Catalog;

public sealed class CatalogModule : IHostPluginModule
{
    public string Key => "vortex-catalog";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.Configure<CatalogConfig>(
            builder.Configuration.GetSection(CatalogConfig.SECTION_NAME)
        );

        services.AddSingleton<ICatalogService, CatalogService>();
        services.AddSingleton<ICatalogAdminService, CatalogAdminService>();
        services.AddSingleton<ITargetedOfferAdminService, TargetedOfferAdminService>();
        services.AddSingleton<ILtdScheduleService, LtdScheduleService>();
        services.AddSingleton<ICatalogClubOfferProvider, CatalogClubOfferProvider>();
        services.AddSingleton<ICatalogClubGiftProvider, CatalogClubGiftProvider>();
        services.AddSingleton<ICatalogSnapshotProvider<NormalCatalog>>(
            sp => new CatalogSnapshotProvider<NormalCatalog>(
                sp.GetRequiredService<IDbContextFactory<VortexDbContext>>(),
                sp.GetRequiredService<ILogger<ICatalogSnapshotProvider<NormalCatalog>>>(),
                sp.GetRequiredService<IFurnitureDefinitionProvider>(),
                CatalogType.Normal
            )
        );
        services.AddSingleton<ICatalogSnapshotProvider<BuildersClubCatalog>>(
            sp => new CatalogSnapshotProvider<BuildersClubCatalog>(
                sp.GetRequiredService<IDbContextFactory<VortexDbContext>>(),
                sp.GetRequiredService<ILogger<ICatalogSnapshotProvider<BuildersClubCatalog>>>(),
                sp.GetRequiredService<IFurnitureDefinitionProvider>(),
                CatalogType.BuildersClub
            )
        );
    }
}
