using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Orleans;
using Turbo.Contracts.Plugins;
using Turbo.Observability.Audit;
using Turbo.Observability.Configuration;
using Turbo.Observability.Context;
using Turbo.Observability.Metrics;
using Turbo.Observability.Runtime;
using Turbo.Primitives.Observability;

namespace Turbo.Observability;

/// <summary>
/// Registers the foundational observability brick: the trace-context accessor (correlation id +
/// Orleans propagation + logging scope + activity), the metrics façade, the audit abstraction
/// (no-op default) and the incoming grain-call filter. It deliberately registers no dashboard,
/// exporter or database writer yet — those arrive in later phases.
/// </summary>
public sealed class ObservabilityModule : IHostPluginModule
{
    public string Key => "turbo-observability";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.Configure<ObservabilityConfig>(
            builder.Configuration.GetSection(ObservabilityConfig.SECTION_NAME)
        );

        services.AddMetrics();

        services.TryAddSingleton<ITurboContextAccessor, TurboContextAccessor>();
        services.TryAddSingleton<ITurboMetrics, TurboMetrics>();

        // Durable observability: one bounded channel -> single background writer (no DB on the hot path).
        services.AddSingleton<AuditChannel>();
        services.TryAddSingleton<IAuditSink, ChannelAuditSink>();
        services.TryAddSingleton<IEconomyLedger, ChannelEconomyLedger>();
        services.TryAddSingleton<IItemForensics, ChannelItemForensics>();
        services.AddHostedService<AuditWriterService>();

        // Orleans resolves all registered IIncomingGrainCallFilter instances from the container.
        services.AddSingleton<IIncomingGrainCallFilter, ObservabilityGrainCallFilter>();
    }
}
