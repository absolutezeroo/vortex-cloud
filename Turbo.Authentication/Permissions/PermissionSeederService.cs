using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Turbo.Database.Context;
using Turbo.Database.Entities.Permissions;

namespace Turbo.Authentication.Permissions;

/// <summary>
/// Bootstraps the default roles into a fresh database at startup. Idempotent and additive-only: a
/// role's capabilities are seeded once, at creation; existing roles are left exactly as the
/// administrator configured them. Failures are logged, never fatal (e.g. before the migration runs).
/// </summary>
internal sealed class PermissionSeederService(
    IDbContextFactory<TurboDbContext> dbContextFactory,
    ILogger<PermissionSeederService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var db = await dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            try
            {
                foreach (var seed in DefaultRoles.All)
                {
                    var exists = await db
                        .Roles.AsNoTracking()
                        .AnyAsync(r => r.Key == seed.Key, cancellationToken)
                        .ConfigureAwait(false);

                    if (exists)
                        continue;

                    var role = new RoleEntity { Key = seed.Key, Name = seed.Name };
                    db.Roles.Add(role);
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    foreach (var capability in seed.Capabilities)
                    {
                        db.RolePermissions.Add(
                            new RolePermissionEntity
                            {
                                RoleEntityId = role.Id,
                                CapabilityKey = capability,
                            }
                        );
                    }

                    if (seed.Capabilities.Count > 0)
                        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    logger.LogInformation(
                        "Seeded default role '{RoleKey}' with {CapabilityCount} capabilities",
                        seed.Key,
                        seed.Capabilities.Count
                    );
                }
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
                "Permission role seeding failed; ensure the permissions migration has been applied."
            );
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
