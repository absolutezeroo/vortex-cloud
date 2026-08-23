using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Primitives.Groups.Providers;
using Vortex.Primitives.Hosting;
using Vortex.Primitives.Plugins;
using Vortex.Social.Configuration;
using Vortex.Social.Providers;

namespace Vortex.Social;

/// <summary>
/// What the social module registers on its own behalf. Split out of PlayerModule with the code it
/// serves: a module that registers another module's services is how the Players bucket grew in the
/// first place.
/// </summary>
public sealed class SocialModule : IHostPluginModule
{
    public string Key => "vortex-social";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.Configure<MessengerConfig>(
            builder.Configuration.GetSection(MessengerConfig.SECTION_NAME)
        );

        services.AddSingleton<IGroupBadgePartProvider, GroupBadgePartProvider>();
        services.AddSingleton<IReferenceDataProvider>(sp =>
            (IReferenceDataProvider)sp.GetRequiredService<IGroupBadgePartProvider>()
        );
    }
}
