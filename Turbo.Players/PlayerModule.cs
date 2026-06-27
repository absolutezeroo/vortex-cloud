using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Turbo.Players.Configuration;
using Turbo.Players.Providers;
using Turbo.Primitives.Groups.Providers;
using Turbo.Primitives.Pets.Providers;
using Turbo.Primitives.Players;
using Turbo.Primitives.Players.Providers;
using Turbo.Primitives.Plugins;

namespace Turbo.Players;

public sealed class PlayerModule : IHostPluginModule
{
    public string Key => "turbo-players";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.Configure<ClubConfig>(builder.Configuration.GetSection(ClubConfig.SECTION_NAME));

        services.AddSingleton<ICurrencyTypeProvider, CurrencyTypeProvider>();
        services.AddSingleton<IGroupBadgePartProvider, GroupBadgePartProvider>();
        services.AddSingleton<IPetPaletteProvider, PetPaletteProvider>();
        services.AddSingleton<IPetCommandProvider, PetCommandProvider>();
        services.AddSingleton<IPetLevelProvider, PetLevelProvider>();
        services.AddSingleton<IPlayerService, PlayerService>();
    }
}
