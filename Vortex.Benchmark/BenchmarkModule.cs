using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Primitives.Benchmark;
using Vortex.Primitives.Hosting;
using Vortex.Primitives.Plugins;

namespace Vortex.Benchmark;

public sealed class BenchmarkModule : IHostPluginModule
{
    public string Key => "vortex-benchmark";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.AddSingleton<BenchmarkProvisioner>();
        services.AddSingleton<BenchmarkService>();
        services.AddSingleton<IBenchmarkService>(sp => sp.GetRequiredService<BenchmarkService>());

        // Registered as hosted too, for the startup sweep -- the same instance, so a run in flight
        // and the sweep can never be two different objects disagreeing about what exists.
        services.AddHostedService(sp => sp.GetRequiredService<BenchmarkService>());
    }
}
