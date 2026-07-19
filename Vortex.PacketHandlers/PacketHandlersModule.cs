using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.PacketHandlers.Configuration;
using Vortex.Primitives.Plugins;

namespace Vortex.PacketHandlers;

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
    }
}
