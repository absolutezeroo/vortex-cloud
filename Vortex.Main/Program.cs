using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vortex.Authentication;
using Vortex.Catalog;
using Vortex.Crypto.Extensions;
using Vortex.Dashboard.API;
using Vortex.Database.Extensions;
using Vortex.Events.Extensions;
using Vortex.Furniture;
using Vortex.Inventory;
using Vortex.Logging.Extensions;
using Vortex.Main.Console;
using Vortex.Main.Extensions;
using Vortex.Marketplace;
using Vortex.Messages.Extensions;
using Vortex.Navigator;
using Vortex.Networking.Extensions;
using Vortex.Observability;
using Vortex.PacketHandlers;
using Vortex.Players;
using Vortex.Plugins.Extensions;
using Vortex.Revisions.Extensions;
using Vortex.Rooms;
using Vortex.Runtime.AssemblyProcessing;
using Vortex.WebApi;

namespace Vortex.Main;

internal class Program
{
    public static async Task Main(string[] args)
    {
        ILogger bootstrapLogger = LoggerFactory
            .Create(builder =>
            {
                builder.ClearProviders();
                builder.AddVortexConsoleLogger();
            })
            .CreateLogger("Bootstrap");

        System.Console.WriteLine(
            @"
            ██╗   ██╗ ██████╗ ██████╗ ████████╗███████╗██╗  ██╗
            ██║   ██║██╔═══██╗██╔══██╗╚══██╔══╝██╔════╝╚██╗██╔╝
            ██║   ██║██║   ██║██████╔╝   ██║   █████╗   ╚███╔╝
            ╚██╗ ██╔╝██║   ██║██╔══██╗   ██║   ██╔══╝   ██╔██╗
             ╚████╔╝ ╚██████╔╝██║  ██║   ██║   ███████╗██╔╝ ██╗
              ╚═══╝   ╚═════╝ ╚═╝  ╚═╝   ╚═╝   ╚══════╝╚═╝  ╚═╝
            "
        );

        bootstrapLogger.LogInformation(
            "Starting {GetProjectName} {GetProductVersion}",
            GetProjectName(),
            GetProjectVersion()
        );

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Configuration.AddEnvironmentVariables(prefix: "VORTEX__");

        if (builder.Environment.IsDevelopment())
        {
            bootstrapLogger.LogInformation("=== Configuration Providers ===");
            foreach (
                IConfigurationProvider p in ((IConfigurationRoot)builder.Configuration).Providers
            )
            {
                if (p is JsonConfigurationProvider jp)
                {
                    JsonConfigurationSource src = (JsonConfigurationSource)jp.Source;
                    string? path = src.Path;

                    if (path is not null)
                    {
                        IFileProvider? fileProvider =
                            src.FileProvider ?? builder.Environment.ContentRootFileProvider;
                        IFileInfo? fi = fileProvider?.GetFileInfo(path);
                        string physical = fi?.PhysicalPath ?? "<virtual or unresolved>";

                        bootstrapLogger.LogInformation($"Json: '{path}' -> {physical}");
                    }
                }
            }
            bootstrapLogger.LogInformation("===============================");
        }

        builder.AddOrleans();

        builder.Services.AddVortexLogging(builder);
        builder.Services.AddVortexNetworking(builder);
        builder.Services.AddVortexPlugins(builder);
        builder.Services.AddVortexDatabaseContext(builder);
        builder.Services.AddVortexEventSystem();
        builder.Services.AddVortexMessageSystem();
        builder.Services.AddVortexCrypto(builder);
        builder.Services.AddVortexRevisions(builder);

        builder.Services.AddHostPlugin<ObservabilityModule>(builder);
        builder.Services.AddHostPlugin<AuthenticationModule>(builder);
        builder.Services.AddHostPlugin<FurnitureModule>(builder);
        builder.Services.AddHostPlugin<CatalogModule>(builder);
        builder.Services.AddHostPlugin<PlayerModule>(builder);
        builder.Services.AddHostPlugin<InventoryModule>(builder);
        builder.Services.AddHostPlugin<MarketplaceModule>(builder);
        builder.Services.AddHostPlugin<DashboardApiModule>(builder);
        builder.Services.AddHostPlugin<NavigatorModule>(builder);
        builder.Services.AddHostPlugin<RoomModule>(builder);
        builder.Services.AddHostPlugin<PacketHandlersModule>(builder);
        builder.Services.AddHostPlugin<WebApiModule>(builder);

        builder.Services.AddSingleton<AssemblyProcessor>();
        builder.Services.AddSingleton<ConsoleCommandService>();

        builder.Services.AddHostedService<VortexEmulator>();

        IHost host = builder.Build();

        IHostApplicationLifetime lifetime =
            host.Services.GetRequiredService<IHostApplicationLifetime>();
        CancellationToken ct = lifetime.ApplicationStopping;

        try
        {
            await host.StartAsync(ct).ConfigureAwait(false);

            bootstrapLogger.LogInformation(
                "Started {GetProjectName} {GetProductVersion}",
                GetProjectName(),
                GetProjectVersion()
            );

            host.Services.GetService<ConsoleCommandService>()?.Enable();

            await host.WaitForShutdownAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            bootstrapLogger.LogCritical(ex, "Host terminated unexpectedly");

            // Fail loudly to the process supervisor (systemd/k8s/container runtime): without a
            // non-zero exit code a fatal startup/runtime failure looks like a clean shutdown and
            // won't trigger a restart or alert.
            System.Environment.ExitCode = 1;
        }
    }

    private static string GetProjectName()
    {
        return "Vortex Emulator";
    }

    public static Version GetProjectVersion()
    {
        return new Version(
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0"
        );
    }
}
