using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
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

        await Task.WhenAll(databaseTask, orleansTask).ConfigureAwait(false);

        var database = await databaseTask.ConfigureAwait(false);
        var orleans = await orleansTask.ConfigureAwait(false);

        return new(Merge(database.Status, orleans.Status), database, orleans);
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

    private async Task<HealthComponentSnapshot> CheckOrleansAsync(CancellationToken ct)
    {
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            var probe = _clusterClient.GetGrain<IPlayerPresenceGrain>(1);
            await probe.IsOnlineAsync(ct).ConfigureAwait(false);
            var latencyMs = GetElapsedMs(startedAt);
            var status = EvaluateStatus(
                latencyMs,
                _orleansDegradedLatencyMs,
                _orleansCriticalLatencyMs
            );
            var detail = $"player-presence grain reachable in {latencyMs:F0}ms.";

            return new("orleans", status.ToString(), detail, Math.Round(latencyMs, 2));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Orleans health probe failed.");

            return new("orleans", HealthStatus.Critical.ToString(), ex.Message, null);
        }
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
    HealthComponentSnapshot Orleans
);

public sealed record HealthComponentSnapshot(
    string Name,
    string Status,
    string Detail,
    double? LatencyMs
);

internal enum HealthStatus
{
    Healthy,
    Degraded,
    Critical,
}
