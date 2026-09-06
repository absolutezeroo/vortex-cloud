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

    /// <remarks>
    /// Nothing. The admin surface was the only thing this assembly registered, and it moved to the
    /// dashboard with the others — authoring content is its job, not the emulator's. The module
    /// stays because <c>PluginBootstrapper</c> scans the assembly of every registered module for
    /// handlers and serializers, which is what makes fishing's own reachable at all.
    /// </remarks>
    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder) { }
}
