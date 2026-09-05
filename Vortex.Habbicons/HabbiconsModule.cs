using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Habbicons.Admin;
using Vortex.Primitives.Habbicons;
using Vortex.Primitives.Hosting;
using Vortex.Primitives.Plugins;

namespace Vortex.Habbicons;

/// <summary>
/// What the Habbicon module registers: the in-process definition catalog (as a reference-data
/// provider, so it loads with the others at startup) and the admin service the dashboard writes
/// content through.
/// </summary>
/// <remarks>
/// The grains are not registered here — Orleans discovers them from the assembly. Only the two
/// singletons need saying out loud.
/// </remarks>
public sealed class HabbiconsModule : IHostPluginModule
{
    public string Key => "vortex-habbicons";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.AddSingleton<HabbiconCatalog>();
        services.AddSingleton<IHabbiconCatalog>(sp => sp.GetRequiredService<HabbiconCatalog>());
        services.AddSingleton<IReferenceDataProvider>(sp =>
            sp.GetRequiredService<HabbiconCatalog>()
        );

        services.AddSingleton<IHabbiconAdminService, HabbiconAdminService>();
    }
}
