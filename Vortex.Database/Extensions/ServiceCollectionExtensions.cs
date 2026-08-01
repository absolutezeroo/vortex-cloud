using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vortex.Database.Configuration;
using Vortex.Database.Context;
using Vortex.Database.Delegates;
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

        services.AddDbContextFactory<VortexDbContext>(
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
            }
        );

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
