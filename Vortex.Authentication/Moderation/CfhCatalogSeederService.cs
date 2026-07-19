using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Moderation;
using Vortex.Primitives.Permissions;

namespace Vortex.Authentication.Moderation;

/// <summary>
/// Bootstraps <see cref="DefaultCfhCatalog"/> into a fresh database at startup. Gated on the whole
/// catalog being empty (not a per-topic check like <see cref="Permissions.PermissionSeederService"/>)
/// — once an admin has touched the catalog at all, it's entirely theirs to restructure.
/// </summary>
internal sealed class CfhCatalogSeederService(
    IDbContextFactory<TurboDbContext> dbContextFactory,
    ILogger<CfhCatalogSeederService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            TurboDbContext db = await dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                bool alreadySeeded = await db
                    .CfhCategories.AsNoTracking()
                    .AnyAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (alreadySeeded)
                {
                    return;
                }

                int categoryOrder = 0;

                foreach (DefaultCfhCatalog.CategorySeed categorySeed in DefaultCfhCatalog.All)
                {
                    CfhCategoryEntity category = new()
                    {
                        Name = categorySeed.Name,
                        DisplayOrder = categoryOrder++,
                    };
                    db.CfhCategories.Add(category);
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    int topicOrder = 0;

                    foreach (DefaultCfhCatalog.TopicSeed topicSeed in categorySeed.Topics)
                    {
                        int? presetId = null;

                        if (topicSeed.BanPresetIndex is int presetIndex)
                        {
                            presetId = await db
                                .SanctionPresets.AsNoTracking()
                                .Where(p =>
                                    p.Kind == SanctionPresetKind.Ban && p.PresetIndex == presetIndex
                                )
                                .Select(p => (int?)p.Id)
                                .FirstOrDefaultAsync(cancellationToken)
                                .ConfigureAwait(false);
                        }

                        db.CfhTopics.Add(
                            new CfhTopicEntity
                            {
                                CfhCategoryEntityId = category.Id,
                                Name = topicSeed.Name,
                                Consequence = topicSeed.Consequence,
                                DefaultSanctionPresetEntityId = presetId,
                                DisplayOrder = topicOrder++,
                            }
                        );
                    }

                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                logger.LogInformation("Seeded default CFH category/topic catalog");
            }
            finally
            {
                await db.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "CFH catalog seeding failed; ensure the cfh_tickets migration has been applied."
            );
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
