using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Turbo.PacketHandlers.Configuration;
using Turbo.Primitives.Plugins;

namespace Turbo.PacketHandlers;

public sealed class PacketHandlersModule : IHostPluginModule
{
    public string Key => "turbo-packet-handlers";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.Configure<FriendListConfig>(
            builder.Configuration.GetSection(FriendListConfig.SECTION_NAME)
        );
        services.Configure<ModerationConfig>(
            builder.Configuration.GetSection(ModerationConfig.SECTION_NAME)
        );
        services.Configure<BadgeConfig>(builder.Configuration.GetSection(BadgeConfig.SECTION_NAME));
    }
}
