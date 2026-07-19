using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Observability.Metrics;
using Vortex.Primitives.Players.Enums;

namespace Vortex.Observability.Runtime;

/// <summary>
/// Periodically refreshes the active-Habbo-Club gauge from the database. Running off the gameplay
/// hot path keeps the count accurate regardless of which player grains are currently activated
/// (online vs offline), which a delta-counter approach could not guarantee.
/// </summary>
public sealed class ClubMetricsRefreshService(
    IDbContextFactory<VortexDbContext> dbContextFactory,
    ClubMetrics metrics,
    ILogger<ClubMetricsRefreshService> logger
) : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using VortexDbContext dbCtx = await dbContextFactory
                    .CreateDbContextAsync(stoppingToken)
                    .ConfigureAwait(false);

                DateTime now = DateTime.UtcNow;

                int active = await dbCtx
                    .PlayerSubscriptions.CountAsync(
                        s => s.SubscriptionType == SubscriptionType.HabboClub && s.ExpiresAt > now,
                        stoppingToken
                    )
                    .ConfigureAwait(false);

                metrics.SetActiveSubscribers(active);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to refresh active Habbo Club subscriber gauge.");
            }

            try
            {
                await Task.Delay(RefreshInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
