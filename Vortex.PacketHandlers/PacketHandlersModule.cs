using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Primitives.Plugins;

namespace Vortex.PacketHandlers;

public sealed class PacketHandlersModule : IHostPluginModule
{
    public string Key => "turbo-packet-handlers";

    // Tunable moderation/friend-list limits moved to IServerConfigGrain (runtime-editable); nothing
    // left to bind here.
    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder) { }
}
