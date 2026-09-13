using System;
using System.Linq;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vortex.Database.Configuration;
using Vortex.Database.Context;
using Vortex.Database.Delegates;

namespace Vortex.Database.Migrations;

public static class MigrationHelper
{
    /// <summary>
    ///     Applies pending <see cref="VortexDbContext" /> migrations when
    ///     <see cref="DatabaseConfig.MigrateOnStartup" /> is set, and does nothing otherwise.
    ///     Call it before the host starts, so a schema the code does not match stops the boot
    ///     instead of surfacing as a failed query on the first login.
    /// </summary>
    public static async Task ApplyStartupMigrationsAsync(IServiceProvider sp, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(sp);

        if (!sp.GetRequiredService<IOptions<DatabaseConfig>>().Value.MigrateOnStartup)
        {
            return;
        }

        ILogger logger = sp.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(MigrationHelper).FullName!);

        // The pooled factory, not GetRequiredService<VortexDbContext>(): the context is registered
        // through AddPooledDbContextFactory, so resolving it directly throws.
        await using VortexDbContext db = await sp.GetRequiredService<
            IDbContextFactory<VortexDbContext>
        >()
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        string[] pending = (
            await db.Database.GetPendingMigrationsAsync(ct).ConfigureAwait(false)
        ).ToArray();

        if (pending.Length == 0)
        {
            logger.LogInformation("Schema is up to date; no pending migrations.");

            return;
        }

        // Named, not counted. When one of them fails halfway, the history table only records the
        // migrations that committed -- this line is the only place that says what was meant to run.
        logger.LogWarning(
            "Applying {Count} pending migration(s): {Migrations}",
            pending.Length,
            string.Join(", ", pending)
        );

        await db.Database.MigrateAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Applied {Count} pending migration(s).", pending.Length);
    }

    public static async Task MigrateAsync<TContext>(IServiceProvider sp, CancellationToken ct)
        where TContext : DbContext
    {
        using TContext db = sp.GetRequiredService<TContext>();

        AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(db.GetType().Assembly);

        if (alc is not null)
        {
            using (alc.EnterContextualReflection())
            {
                await db.Database.MigrateAsync(ct).ConfigureAwait(false);
            }
        }
        else
        {
            await db.Database.MigrateAsync(ct).ConfigureAwait(false);
        }
    }

    public static async Task UninstallAsync<TContext>(IServiceProvider sp, CancellationToken ct)
        where TContext : DbContext
    {
        using TContext db = sp.GetRequiredService<TContext>();

        TablePrefixProvider prefix = sp.GetRequiredService<TablePrefixProvider>();
        string rawPrefix = prefix();

        // An empty prefix would match every table in the schema and drop the whole database.
        if (string.IsNullOrWhiteSpace(rawPrefix))
        {
            throw new InvalidOperationException(
                "Refusing to uninstall plugin tables: table prefix is empty."
            );
        }

        // Sanitize the prefix for use inside a single-quoted LIKE pattern: escape backslashes
        // and quotes, and neutralize LIKE wildcards so a hostile/malformed prefix cannot widen
        // the destructive scope of the DROP statements.
        string tablePrefix = rawPrefix
            .Replace("\\", "\\\\")
            .Replace("'", "''")
            .Replace("%", "\\%")
            .Replace("_", "\\_");

        // COALESCE guard: GROUP_CONCAT returns NULL when no table matches the prefix, and
        // `PREPARE stmt FROM NULL` errors — so a plugin with no installed tables would fail to
        // uninstall. Fall back to a harmless no-op (`DO 0`) in that case.
        string sql =
            $@"
SET @sql = (
  SELECT GROUP_CONCAT(CONCAT('DROP TABLE IF EXISTS `', TABLE_SCHEMA, '`.`', TABLE_NAME, '`') SEPARATOR ';')
  FROM INFORMATION_SCHEMA.TABLES
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME LIKE '{tablePrefix}%'
);
SET @sql = COALESCE(@sql, 'DO 0');
SET FOREIGN_KEY_CHECKS = 0;
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
SET FOREIGN_KEY_CHECKS = 1;";
        await db.Database.ExecuteSqlRawAsync(sql, ct).ConfigureAwait(false);
    }
}
