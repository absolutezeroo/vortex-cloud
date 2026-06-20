using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Turbo.Primitives.Plugins;

public interface IHostPluginModule
{
    public string Key { get; }
    void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder);
}
