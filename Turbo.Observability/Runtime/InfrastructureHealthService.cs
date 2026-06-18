using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using Turbo.Database.Context;
using Turbo.Observability.Configuration;
using Turbo.Primitives.Players.Grains;

namespace Turbo.Observability.Runtime;

public sealed class InfrastructureHealthService(
    IDbContextFactory<TurboDbContext> dbContextFactory,
    IClusterClient clusterClient,
    IOptions<ObservabilityConfig> config,
    ILogger<InfrastructureHealthService> logger
) : IInfrastructureHealthService
{
    private readonly IDbContextFactory<TurboDbContext> _dbContextFactory = dbContextFactory;
    private readonly IClusterClient _clusterClient = clusterClient;
    private readonly ILogger<InfrastructureHealthService> _logger = logger;
    private readonly int _dbDegradedLatencyMs = Math.Max(1, config.Value.DatabaseProbeDegradedMs);
    private readonly int _dbCriticalLatencyMs = Math.Max(
        Math.Max(1, config.Value.DatabaseProbeDegradedMs),
        config.Value.DatabaseProbeCriticalMs
    );
    private readonly int _orleansDegradedLatencyMs = Math.Max(
        1,
        config.Value.OrleansProbeDegradedMs
    );
    private readonly int _orleansCriticalLatencyMs = Math.Max(
        Math.Max(1, config.Value.OrleansProbeDegradedMs),
        config.Value.OrleansProbeCriticalMs
    );

    public async Task<InfrastructureHealthSnapshot> GetStatusAsync(CancellationToken ct)
    {
        var databaseTask = CheckDatabaseAsync(ct);
        var orleansTask = CheckOrleansAsync(ct);
        var runtime = GetRuntimeSnapshot();

        await Task.WhenAll(databaseTask, orleansTask).ConfigureAwait(false);

        var database = await databaseTask.ConfigureAwait(false);
        var orleans = await orleansTask.ConfigureAwait(false);

        return new(
            Merge(database.Status, orleans.Component.Status),
            database,
            orleans.Component,
            runtime,
            orleans.Cluster
        );
    }

    private async Task<HealthComponentSnapshot> CheckDatabaseAsync(CancellationToken ct)
    {
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            await using var db = await _dbContextFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            var canConnect = await db.Database.CanConnectAsync(ct).ConfigureAwait(false);
            var latencyMs = GetElapsedMs(startedAt);
            var status = canConnect
                ? EvaluateStatus(latencyMs, _dbDegradedLatencyMs, _dbCriticalLatencyMs)
                : HealthStatus.Degraded;
            var detail = canConnect
                ? $"connected in {latencyMs:F0}ms."
                : "CanConnectAsync returned false.";

            return new("database", status.ToString(), detail, Math.Round(latencyMs, 2));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database health probe failed.");

            return new("database", HealthStatus.Critical.ToString(), ex.Message, null);
        }
    }

    private async Task<OrleansHealthProbe> CheckOrleansAsync(CancellationToken ct)
    {
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            var hosts = await _clusterClient
                .GetGrain<IManagementGrain>(0)
                .GetHosts(false)
                .ConfigureAwait(false);

            var latencyMs = GetElapsedMs(startedAt);
            var activeSiloCount = hosts.Count(host => host.Value == SiloStatus.Active);
            var hasInactiveSilo = hosts.Any(host => host.Value != SiloStatus.Active);
            var status =
                activeSiloCount == 0 ? HealthStatus.Critical
                : hasInactiveSilo ? HealthStatus.Degraded
                : EvaluateStatus(latencyMs, _orleansDegradedLatencyMs, _orleansCriticalLatencyMs);
            var detail =
                activeSiloCount > 0
                    ? $"management grain reachable in {latencyMs:F0}ms; {activeSiloCount}/{hosts.Count} active silos."
                    : "management grain reachable, but no active silo was reported.";

            return new(
                new("orleans", status.ToString(), detail, Math.Round(latencyMs, 2)),
                new(
                    status.ToString(),
                    detail,
                    hosts.Count,
                    activeSiloCount,
                    hosts
                        .OrderBy(host => host.Key.ToString())
                        .Select(host => new OrleansSiloSnapshot(
                            host.Key.ToString(),
                            host.Value.ToString()
                        ))
                        .ToArray()
                )
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Orleans health probe failed.");

            return new(
                new("orleans", HealthStatus.Critical.ToString(), ex.Message, null),
                new(
                    HealthStatus.Critical.ToString(),
                    ex.Message,
                    0,
                    0,
                    Array.Empty<OrleansSiloSnapshot>()
                )
            );
        }
    }

    private static RuntimeHealthSnapshot GetRuntimeSnapshot()
    {
        using var process = Process.GetCurrentProcess();
        var startedAtUtc = process.StartTime.ToUniversalTime();
        var uptimeSeconds = Math.Max(0, (long)(DateTime.UtcNow - startedAtUtc).TotalSeconds);
        var environmentName =
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Production";

        return new(
            "Running",
            startedAtUtc,
            uptimeSeconds,
            Environment.MachineName,
            environmentName,
            Environment.ProcessId,
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription,
            Environment.ProcessorCount,
            process.WorkingSet64 / 1024 / 1024,
            GC.GetTotalMemory(false) / 1024 / 1024
        );
    }

    private static string Merge(string left, string right)
    {
        if (
            string.Equals(
                left,
                HealthStatus.Critical.ToString(),
                StringComparison.OrdinalIgnoreCase
            )
            || string.Equals(
                right,
                HealthStatus.Critical.ToString(),
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return HealthStatus.Critical.ToString();
        }

        if (
            string.Equals(
                left,
                HealthStatus.Degraded.ToString(),
                StringComparison.OrdinalIgnoreCase
            )
            || string.Equals(
                right,
                HealthStatus.Degraded.ToString(),
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return HealthStatus.Degraded.ToString();
        }

        return HealthStatus.Healthy.ToString();
    }

    private static HealthStatus EvaluateStatus(
        double latencyMs,
        int degradedThresholdMs,
        int criticalThresholdMs
    )
    {
        if (latencyMs >= criticalThresholdMs)
            return HealthStatus.Critical;

        if (latencyMs >= degradedThresholdMs)
            return HealthStatus.Degraded;

        return HealthStatus.Healthy;
    }

    private static double GetElapsedMs(long startedAtTimestamp) =>
        Stopwatch.GetElapsedTime(startedAtTimestamp).TotalMilliseconds;
}

public interface IInfrastructureHealthService
{
    Task<InfrastructureHealthSnapshot> GetStatusAsync(CancellationToken ct);
}

public sealed record InfrastructureHealthSnapshot(
    string Overall,
    HealthComponentSnapshot Database,
    HealthComponentSnapshot Orleans,
    RuntimeHealthSnapshot Runtime,
    OrleansClusterSnapshot OrleansCluster
);

public sealed record HealthComponentSnapshot(
    string Name,
    string Status,
    string Detail,
    double? LatencyMs
);

public sealed record RuntimeHealthSnapshot(
    string Status,
    DateTime StartedAtUtc,
    long UptimeSeconds,
    string MachineName,
    string EnvironmentName,
    int ProcessId,
    string FrameworkDescription,
    string OsDescription,
    int ProcessorCount,
    long WorkingSetMb,
    long ManagedMemoryMb
);

public sealed record OrleansClusterSnapshot(
    string Status,
    string Detail,
    int SiloCount,
    int ActiveSiloCount,
    IReadOnlyList<OrleansSiloSnapshot> Silos
);

public sealed record OrleansSiloSnapshot(string Address, string Status);

internal sealed record OrleansHealthProbe(
    HealthComponentSnapshot Component,
    OrleansClusterSnapshot Cluster
);

internal enum HealthStatus
{
    Healthy,
    Degraded,
    Critical,
}
