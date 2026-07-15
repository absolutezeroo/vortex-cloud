using System;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Turbo.Database.Delegates;

namespace Turbo.Database.Migrations;

public static class MigrationHelper
{
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
