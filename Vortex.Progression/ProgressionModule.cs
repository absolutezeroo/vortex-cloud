using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Primitives.Hosting;
using Vortex.Primitives.Players.Providers;
using Vortex.Primitives.Plugins;
using Vortex.Primitives.Polls;
using Vortex.Primitives.Prizes;
using Vortex.Primitives.Quests;
using Vortex.Progression.Configuration;
using Vortex.Progression.Polls;
using Vortex.Progression.Prizes;
using Vortex.Progression.Providers;
using Vortex.Progression.Quests;

namespace Vortex.Progression;

/// <summary>
/// What the progression module registers on its own behalf: the achievement batching window, the
/// account level ladder cache, and the four admin services the dashboard writes progression content
/// through.
/// </summary>
public sealed class ProgressionModule : IHostPluginModule
{
    public string Key => "vortex-progression";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.Configure<AchievementConfig>(
            builder.Configuration.GetSection(AchievementConfig.SECTION_NAME)
        );

        services.AddSingleton<AccountLevelProvider>();
        services.AddSingleton<IAccountLevelProvider>(sp =>
            sp.GetRequiredService<AccountLevelProvider>()
        );
        services.AddSingleton<IReferenceDataProvider>(sp =>
            sp.GetRequiredService<AccountLevelProvider>()
        );

        services.AddSingleton<IQuestAdminService, QuestAdminService>();
        services.AddSingleton<IQuestContentAdminService, QuestContentAdminService>();
        services.AddSingleton<IPollAdminService, PollAdminService>();
        services.AddSingleton<IPrizePoolAdminService, PrizePoolAdminService>();
    }
}
