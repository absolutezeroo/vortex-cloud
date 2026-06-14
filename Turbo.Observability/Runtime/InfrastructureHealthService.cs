using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Turbo.Database.Context;
using Turbo.Primitives.Players.Grains;

namespace Turbo.Observability.Runtime;

internal sealed class InfrastructureHealthService(
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
            Merge(database.Overall, orleans.Overall),
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
                return new("database", HealthStatus.Degraded, "CanConnectAsync returned false.");

            return new("database", HealthStatus.Healthy, "connected");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database health probe failed.");

            return new("database", HealthStatus.Critical, ex.Message);
        }
    }

    private async Task<HealthComponentSnapshot> CheckOrleansAsync(CancellationToken ct)
    {
        try
        {
            var probe = _clusterClient.GetGrain<IPlayerPresenceGrain>(1);
            await probe.IsOnlineAsync(ct).ConfigureAwait(false);

            return new("orleans", HealthStatus.Healthy, "player-presence grain reachable");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Orleans health probe failed.");

            return new("orleans", HealthStatus.Critical, ex.Message);
        }
    }

    private static string Merge(HealthStatus left, HealthStatus right)
    {
        if (left == HealthStatus.Critical || right == HealthStatus.Critical)
            return HealthStatus.Critical.ToString().ToLowerInvariant();

        if (left == HealthStatus.Degraded || right == HealthStatus.Degraded)
            return HealthStatus.Degraded.ToString().ToLowerInvariant();

        return HealthStatus.Healthy.ToString().ToLowerInvariant();
    }
}

internal interface IInfrastructureHealthService
{
    Task<InfrastructureHealthSnapshot> GetStatusAsync(CancellationToken ct);
}

internal sealed record InfrastructureHealthSnapshot(
    string Overall,
    HealthComponentSnapshot Database,
    HealthComponentSnapshot Orleans
);

internal sealed record HealthComponentSnapshot(string Name, string Status, string Detail);

internal enum HealthStatus
{
    Healthy,
    Degraded,
    Critical,
}
