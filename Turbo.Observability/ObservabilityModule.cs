using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Orleans;
using Turbo.Observability.Audit;
using Turbo.Observability.Configuration;
using Turbo.Observability.Context;
using Turbo.Observability.ErrorTracking;
using Turbo.Observability.Metrics;
using Turbo.Observability.Runtime;
using Turbo.Primitives.Observability;
using Turbo.Primitives.Plugins;

namespace Turbo.Observability;

/// <summary>
/// Registers the observability module: the trace-context accessor (correlation id + Orleans
/// propagation + logging scope + activity), the metrics façade, the durable audit pipeline
/// (event-handler sinks -> bounded channel -> background writer), the grain-call filter and the
/// native admin dashboard (self-disabled unless configured with a token).
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
        services.TryAddSingleton<ILiveStatsAggregator, LiveStatsAggregator>();
        services.TryAddSingleton<ITurboMetrics, TurboMetrics>();
        services.TryAddSingleton<ClubMetrics>();
        services.AddHostedService<ClubMetricsRefreshService>();
        services.TryAddSingleton<ClientPerformanceMetrics>();
        services.TryAddSingleton<IPerformanceLogSink>(sp =>
            sp.GetRequiredService<ClientPerformanceMetrics>()
        );
        services.TryAddSingleton<IInfrastructureHealthService, InfrastructureHealthService>();
        services.TryAddSingleton<IIncidentDetectionService, IncidentDetectionService>();

        // Error-grouping pipeline: bounded channel -> single background writer (no DB on the hot path).
        services.AddSingleton<ErrorGroupingChannel>();
        services.TryAddSingleton<IErrorGroupingSink, ChannelErrorGroupingSink>();
        services.AddHostedService<ErrorGroupingWriterService>();

        // Durable audit pipeline: one bounded channel -> single background writer (no DB on the hot path).
        services.AddSingleton<AuditChannel>();
        services.TryAddSingleton<IAuditSink, ChannelAuditSink>();
        services.TryAddSingleton<IEconomyLedger, ChannelEconomyLedger>();
        services.TryAddSingleton<IItemForensics, ChannelItemForensics>();
        services.AddHostedService<AuditWriterService>();

        // Orleans resolves all registered IIncomingGrainCallFilter instances from the container.
        services.AddSingleton<IIncomingGrainCallFilter, ObservabilityGrainCallFilter>();
    }
}
