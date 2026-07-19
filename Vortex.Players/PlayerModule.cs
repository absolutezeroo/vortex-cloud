using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Players.Configuration;
using Vortex.Players.Providers;
using Vortex.Primitives.Groups.Providers;
using Vortex.Primitives.Pets.Providers;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Providers;
using Vortex.Primitives.Plugins;

namespace Vortex.Players;

public sealed class PlayerModule : IHostPluginModule
{
    public string Key => "turbo-players";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.Configure<ClubConfig>(builder.Configuration.GetSection(ClubConfig.SECTION_NAME));
        services.Configure<MessengerConfig>(
            builder.Configuration.GetSection(MessengerConfig.SECTION_NAME)
        );
        services.Configure<GroupConfig>(builder.Configuration.GetSection(GroupConfig.SECTION_NAME));
        services.Configure<PlayerPresenceConfig>(
            builder.Configuration.GetSection(PlayerPresenceConfig.SECTION_NAME)
        );

        services.AddSingleton<ICurrencyTypeProvider, CurrencyTypeProvider>();
        services.AddSingleton<IGroupBadgePartProvider, GroupBadgePartProvider>();
        services.AddSingleton<IPetPaletteProvider, PetPaletteProvider>();
        services.AddSingleton<IPetCommandProvider, PetCommandProvider>();
        services.AddSingleton<IPetLevelProvider, PetLevelProvider>();
        services.AddSingleton<IBuildersClubService, BuildersClubService>();
        services.AddHostedService<BuildersClubTierSeederService>();
    }
}
