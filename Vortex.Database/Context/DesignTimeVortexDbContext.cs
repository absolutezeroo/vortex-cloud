using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Vortex.Database.Context
{
    public class VortexDbContextFactory : IDesignTimeDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<VortexDbContext> optionsBuilder =
                new DbContextOptionsBuilder<VortexDbContext>();

            string basePath = Path.Combine(Directory.GetCurrentDirectory(), "..");
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            string? connectionString = configuration["Vortex:Database:ConnectionString"];

            // AutoDetect requires a live MySQL connection, which is unavailable when authoring
            // migrations offline. Allow pinning the server version via configuration
            // (Vortex:Database:ServerVersion, e.g. "8.0.32-mysql") to bypass detection.
            string? versionConfig = configuration["Vortex:Database:ServerVersion"];
            ServerVersion? serverVersion = string.IsNullOrWhiteSpace(versionConfig)
                ? ServerVersion.AutoDetect(connectionString)
                : ServerVersion.Parse(versionConfig);

            optionsBuilder.UseMySql(
                connectionString,
                serverVersion,
                options =>
                {
                    options.MigrationsAssembly("Vortex.Database");
                }
            );

            return new VortexDbContext(optionsBuilder.Options);
        }
    }
}
