using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Database.Context;
using Turbo.Primitives.Players.Grains;

namespace Turbo.Observability.Runtime;

public sealed class InfrastructureHealthService(
    IDbContextFactory<TurboDbContext> dbContextFactory,
    IClusterClient clusterClient,
    ILogger<InfrastructureHealthService> logger
) : IInfrastructureHealthService
{
    private readonly IDbContextFactory<TurboDbContext> _dbContextFactory = dbContextFactory;
    private readonly IClusterClient _clusterClient = clusterClient;
    private readonly ILogger<InfrastructureHealthService> _logger = logger;

    public async Task<InfrastructureHealthSnapshot> GetStatusAsync(CancellationToken ct)
    {
        var databaseTask = CheckDatabaseAsync(ct);
        var orleansTask = CheckOrleansAsync(ct);

        await Task.WhenAll(databaseTask, orleansTask).ConfigureAwait(false);

        var database = await databaseTask.ConfigureAwait(false);
        var orleans = await orleansTask.ConfigureAwait(false);

        return new(
            Merge(database.Status, orleans.Status),
            database,
            orleans
        );
    }

    private async Task<HealthComponentSnapshot> CheckDatabaseAsync(CancellationToken ct)
    {
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            var canConnect = await db.Database.CanConnectAsync(ct).ConfigureAwait(false);

            if (!canConnect)
                return new("database", HealthStatus.Degraded.ToString().ToLowerInvariant(), "CanConnectAsync returned false.");

            return new("database", HealthStatus.Healthy.ToString().ToLowerInvariant(), "connected");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database health probe failed.");

            return new("database", HealthStatus.Critical.ToString().ToLowerInvariant(), ex.Message);
        }
    }

    private async Task<HealthComponentSnapshot> CheckOrleansAsync(CancellationToken ct)
    {
        try
        {
            var probe = _clusterClient.GetGrain<IPlayerPresenceGrain>(1);
            await probe.IsOnlineAsync(ct).ConfigureAwait(false);

            return new("orleans", HealthStatus.Healthy.ToString().ToLowerInvariant(), "player-presence grain reachable");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Orleans health probe failed.");

            return new("orleans", HealthStatus.Critical.ToString().ToLowerInvariant(), ex.Message);
        }
    }

    private static string Merge(string left, string right)
    {
        var normalizedLeft = NormalizeStatus(left);
        var normalizedRight = NormalizeStatus(right);

        if (normalizedLeft == HealthStatus.Critical || normalizedRight == HealthStatus.Critical)
            return HealthStatus.Critical.ToString().ToLowerInvariant();

        if (normalizedLeft == HealthStatus.Degraded || normalizedRight == HealthStatus.Degraded)
            return HealthStatus.Degraded.ToString().ToLowerInvariant();

        return HealthStatus.Healthy.ToString().ToLowerInvariant();
    }

    private static HealthStatus NormalizeStatus(string value) =>
        value.Equals("critical", StringComparison.OrdinalIgnoreCase)
            ? HealthStatus.Critical
            : value.Equals("degraded", StringComparison.OrdinalIgnoreCase)
                ? HealthStatus.Degraded
                : HealthStatus.Healthy;
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

public sealed record HealthComponentSnapshot(string Name, string Status, string Detail);

internal enum HealthStatus
{
    Healthy,
    Degraded,
    Critical,
}
