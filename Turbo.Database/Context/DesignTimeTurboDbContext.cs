using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Turbo.Database.Context
{
    public class TurboDbContextFactory : IDesignTimeDbContextFactory<TurboDbContext>
    {
        public TurboDbContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<TurboDbContext> optionsBuilder = new DbContextOptionsBuilder<TurboDbContext>();

            string basePath = Path.Combine(Directory.GetCurrentDirectory(), "..");
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            string? connectionString = configuration["Turbo:Database:ConnectionString"];

            // AutoDetect requires a live MySQL connection, which is unavailable when authoring
            // migrations offline. Allow pinning the server version via configuration
            // (Turbo:Database:ServerVersion, e.g. "8.0.32-mysql") to bypass detection.
            string? versionConfig = configuration["Turbo:Database:ServerVersion"];
            ServerVersion? serverVersion = string.IsNullOrWhiteSpace(versionConfig)
                ? ServerVersion.AutoDetect(connectionString)
                : ServerVersion.Parse(versionConfig);

            optionsBuilder.UseMySql(
                connectionString,
                serverVersion,
                options =>
                {
                    options.MigrationsAssembly("Turbo.Database");
                }
            );

            return new TurboDbContext(optionsBuilder.Options);
        }
    }
}
