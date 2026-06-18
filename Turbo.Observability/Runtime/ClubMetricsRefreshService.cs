using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Turbo.Database.Context;
using Turbo.Observability.Metrics;
using Turbo.Primitives.Players.Enums;

namespace Turbo.Observability.Runtime;

/// <summary>
/// Periodically refreshes the active-Habbo-Club gauge from the database. Running off the gameplay
/// hot path keeps the count accurate regardless of which player grains are currently activated
/// (online vs offline), which a delta-counter approach could not guarantee.
/// </summary>
public sealed class ClubMetricsRefreshService(
    IDbContextFactory<TurboDbContext> dbContextFactory,
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
                await using var dbCtx = await dbContextFactory
                    .CreateDbContextAsync(stoppingToken)
                    .ConfigureAwait(false);

                var now = DateTime.UtcNow;

                var active = await dbCtx
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
