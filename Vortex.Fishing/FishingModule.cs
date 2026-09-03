using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Primitives.Fishing;
using Vortex.Primitives.Plugins;

namespace Vortex.Fishing;

/// <summary>
/// Fishing's host wiring. It had none: the grains are found by Orleans on their own, so nothing in
/// this assembly needed registering until the admin surface arrived.
/// </summary>
public sealed class FishingModule : IHostPluginModule
{
    public string Key => "vortex-fishing";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder) =>
        services.AddSingleton<IFishingAdminService, FishingAdminService>();
}
