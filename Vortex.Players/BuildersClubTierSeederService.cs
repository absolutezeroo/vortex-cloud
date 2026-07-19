using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Players;

namespace Vortex.Players;

/// <summary>
/// Bootstraps default Builders Club furni-limit tiers into a fresh database. Idempotent and
/// additive-only per level, same convention as SanctionPresetSeederService: once an admin has
/// edited a tier's limit, it's theirs and never overwritten.
/// </summary>
internal sealed class BuildersClubTierSeederService(
    IDbContextFactory<TurboDbContext> dbContextFactory,
    ILogger<BuildersClubTierSeederService> logger
) : IHostedService
{
    private static readonly (int Level, int FurniLimit)[] DefaultTiers = [(1, 300), (2, 600)];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        TurboDbContext db = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            int seeded = 0;

            foreach ((int level, int furniLimit) in DefaultTiers)
            {
                bool exists = await db
                    .BuildersClubTiers.AsNoTracking()
                    .AnyAsync(t => t.Level == level, cancellationToken)
                    .ConfigureAwait(false);

                if (exists)
                {
                    continue;
                }

                db.BuildersClubTiers.Add(
                    new BuildersClubTierEntity { Level = level, FurniLimit = furniLimit }
                );
                seeded++;
            }

            if (seeded > 0)
            {
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Seeded {Count} default Builders Club tier(s)", seeded);
            }
        }
        finally
        {
            await db.DisposeAsync().ConfigureAwait(false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
