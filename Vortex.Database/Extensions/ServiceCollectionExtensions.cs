using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vortex.Database.Auditing;
using Vortex.Database.Backup;
using Vortex.Database.Commerce;
using Vortex.Database.Configuration;
using Vortex.Database.Context;
using Vortex.Database.Delegates;
using Vortex.Primitives.Commerce;
using Vortex.Primitives.Plugins;

namespace Vortex.Database.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVortexDatabaseContext(
        this IServiceCollection services,
        HostApplicationBuilder builder
    )
    {
        // Validated at start so a missing or placeholder connection string is one clear startup
        // error instead of every query failing later at the driver level.
        services
            .AddOptions<DatabaseConfig>()
            .Bind(builder.Configuration.GetSection(DatabaseConfig.SECTION_NAME))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<DatabaseConfig>, DatabaseConfigValidator>();

        // Pooled: every caller here creates a context per operation and disposes it, which is the
        // exact lifetime pooling is built for, and the context takes nothing but its options so it
        // resets cleanly. Measured at 20,000 create/dispose cycles: 1,085ms unpooled against 56ms
        // pooled, so roughly 54us per context down to 2.8us. That isolates construction alone --
        // a real query still pays for its connection and its round trip, so the share of an
        // operation this wins back shrinks the more work the operation actually does.
        // ponytail: default pool size (1024); raise it only if a caller is ever found holding a
        // context across awaits and starving the pool.
        services.AddPooledDbContextFactory<VortexDbContext>(
            (sp, options) =>
            {
                DatabaseConfig dbConfig = sp.GetRequiredService<IOptions<DatabaseConfig>>().Value;
                string connectionString = dbConfig.ConnectionString;

                options.UseMySql(
                    connectionString,
                    ResolveServerVersion(dbConfig, connectionString),
                    options =>
                    {
                        options.MigrationsAssembly("Vortex.Database");
                        options.EnableRetryOnFailure(maxRetryCount: 3);
                    }
                );

                // Records what an admin write replaced, read from EF's own original values. Inert
                // unless an EntityChangeCapture is armed, which only the dashboard's operation
                // envelope does -- the game's write path never enters the body of the interceptor.
                options.AddInterceptors(new EntityChangeInterceptor());
            }
        );

        services
            .AddOptions<DatabaseBackupConfig>()
            .Bind(builder.Configuration.GetSection(DatabaseBackupConfig.SECTION_NAME));

        // The durable record every value-moving flow writes its progress to. Stateless over the
        // context factory, so a singleton.
        services.AddSingleton<ICommerceJournal, CommerceJournal>();

        services
            .AddOptions<CommerceRecoveryConfig>()
            .Bind(builder.Configuration.GetSection(CommerceRecoveryConfig.SECTION_NAME));

        // Publishes the critical events completed operations still owe, and escalates anything that
        // has been stuck past its pivot. Finds nothing on a healthy hotel, which is the point.
        services.AddHostedService<CommerceRelayService>();

        services.AddSingleton<IDatabaseBackupService, DatabaseBackupService>();
        services.AddHostedService<DatabaseBackupScheduler>();

        return services;
    }

    private static ServerVersion ResolveServerVersion(
        DatabaseConfig dbConfig,
        string connectionString
    ) =>
        string.IsNullOrWhiteSpace(dbConfig.MySqlServerVersion)
            ? ServerVersion.AutoDetect(connectionString)
            : ServerVersion.Parse(dbConfig.MySqlServerVersion);

    public static IServiceCollection AddPluginTablePrefix<TContext>(
        this IServiceCollection services
    )
        where TContext : DbContext
    {
        services.AddSingleton<TablePrefixProvider>(sp =>
        {
            PluginManifest manifest = sp.GetRequiredService<PluginManifest>();

            string tablePrefix = manifest.TablePrefix ?? string.Empty;

            if (manifest.ExplicitlyNoTablePrefix ?? false)
            {
                tablePrefix = string.Empty;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(tablePrefix))
                {
                    tablePrefix = new string(
                        manifest
                            .Key.Split('-')
                            .Where(part => !string.IsNullOrEmpty(part))
                            .Select(part => char.ToLowerInvariant(part[0]))
                            .ToArray()
                    );
                }

                tablePrefix += "_";
            }

            return () => tablePrefix;
        });

        return services;
    }

    public static IServiceCollection AddPluginDatabaseContext<TContext, TModule>(
        this IServiceCollection services
    )
        where TContext : DbContext
        where TModule : class, IPluginDbModule
    {
        services.AddPluginTablePrefix<TContext>();
        services.AddTransient<IPluginDbModule, TModule>();

        services.AddDbContext<TContext>(
            (sp, options) =>
            {
                TablePrefixProvider prefix = sp.GetRequiredService<TablePrefixProvider>();
                IHostServices host = sp.GetRequiredService<IHostServices>();
                DatabaseConfig dbConfig = host.GetRequiredService<IOptions<DatabaseConfig>>().Value;
                string connectionString = dbConfig.ConnectionString;

                options.UseMySql(
                    connectionString,
                    ResolveServerVersion(dbConfig, connectionString),
                    builder =>
                    {
                        builder.MigrationsHistoryTable(
                            $"__EFMigrationsHistory_{prefix().TrimEnd('_')}"
                        );
                        builder.EnableRetryOnFailure(maxRetryCount: 3);
                    }
                );
            }
        );

        return services;
    }
}
