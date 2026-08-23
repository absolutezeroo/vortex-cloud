using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Players.Configuration;
using Vortex.Players.Content;
using Vortex.Players.MysteryBox;
using Vortex.Players.Polls;
using Vortex.Players.Prizes;
using Vortex.Players.Providers;
using Vortex.Players.Quests;
using Vortex.Primitives.Content;
using Vortex.Primitives.Hosting;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.MysteryBox;
using Vortex.Primitives.Pets.Providers;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Providers;
using Vortex.Primitives.Plugins;
using Vortex.Primitives.Polls;
using Vortex.Primitives.Prizes;
using Vortex.Primitives.Quests;

namespace Vortex.Players;

public sealed class PlayerModule : IHostPluginModule
{
    public string Key => "turbo-players";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.Configure<AchievementConfig>(
            builder.Configuration.GetSection(AchievementConfig.SECTION_NAME)
        );
        services.Configure<ClubConfig>(builder.Configuration.GetSection(ClubConfig.SECTION_NAME));
        services.Configure<PlayerPresenceConfig>(
            builder.Configuration.GetSection(PlayerPresenceConfig.SECTION_NAME)
        );

        services.AddSingleton<IUserClassificationService, UserClassificationService>();
        services.AddSingleton<ICurrencyTypeProvider, CurrencyTypeProvider>();
        services.AddSingleton<IReferenceDataProvider>(sp =>
            (IReferenceDataProvider)sp.GetRequiredService<ICurrencyTypeProvider>()
        );
        services.AddSingleton<IPetPaletteProvider, PetPaletteProvider>();
        services.AddSingleton<IReferenceDataProvider>(sp =>
            (IReferenceDataProvider)sp.GetRequiredService<IPetPaletteProvider>()
        );
        services.AddSingleton<IPetCommandProvider, PetCommandProvider>();
        services.AddSingleton<IReferenceDataProvider>(sp =>
            (IReferenceDataProvider)sp.GetRequiredService<IPetCommandProvider>()
        );
        services.AddSingleton<IPetLevelProvider, PetLevelProvider>();
        services.AddSingleton<IReferenceDataProvider>(sp =>
            (IReferenceDataProvider)sp.GetRequiredService<IPetLevelProvider>()
        );
        services.AddSingleton<IPetVocalProvider, PetVocalProvider>();
        services.AddSingleton<IReferenceDataProvider>(sp =>
            (IReferenceDataProvider)sp.GetRequiredService<IPetVocalProvider>()
        );
        services.AddSingleton<AccountLevelProvider>();
        services.AddSingleton<IAccountLevelProvider>(sp =>
            sp.GetRequiredService<AccountLevelProvider>()
        );
        services.AddSingleton<IReferenceDataProvider>(sp =>
            sp.GetRequiredService<AccountLevelProvider>()
        );
        services.AddSingleton<IBuildersClubService, BuildersClubService>();
        services.AddSingleton<IQuestAdminService, QuestAdminService>();
        services.AddSingleton<IPollAdminService, PollAdminService>();
        services.AddSingleton<IQuestContentAdminService, QuestContentAdminService>();
        services.AddSingleton<IMysteryBoxAdminService, MysteryBoxAdminService>();
        services.AddSingleton<IPrizePoolAdminService, PrizePoolAdminService>();
        services.AddSingleton<IContentAdminService, ContentAdminService>();
        services.AddHostedService<BuildersClubTierSeederService>();
    }
}
