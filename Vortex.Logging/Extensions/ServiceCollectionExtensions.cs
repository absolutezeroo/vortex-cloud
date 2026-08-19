using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vortex.Logging.Factories;
using Vortex.Primitives.Console;

namespace Vortex.Logging.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     How much console history a newly opened dashboard console replays. Deep enough to cover a
    ///     startup sequence, shallow enough that the buffer is not a memory concern.
    /// </summary>
    private const int CONSOLE_FEED_LINES = 2000;

    public static IServiceCollection AddVortexLogging(
        this IServiceCollection services,
        HostApplicationBuilder builder
    )
    {
        services.Configure<VortexConsoleFormatterOptions>(
            builder.Configuration.GetSection(VortexConsoleFormatterOptions.SECTION_NAME)
        );

        builder.Logging.ClearProviders();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        builder.Logging.AddVortexConsoleLogger();

        // A second sink onto the same lines the terminal gets, so the dashboard can show the real
        // console rather than a reconstruction of it. Registered as ILoggerProvider so the logging
        // factory applies the configured level rules to it exactly as it does to the console.
        services.AddSingleton(new ServerConsoleFeed(CONSOLE_FEED_LINES));
        services.AddSingleton<ILoggerProvider, ServerConsoleLoggerProvider>();

        return services;
    }

    public static IServiceCollection ConfigurePrefixedLogging(
        this IServiceCollection services,
        IServiceProvider host,
        string prefix
    )
    {
        services.AddSingleton<IPrefixedLoggerFactory>(sp => new PrefixedLoggerFactory(
            host.GetRequiredService<ILoggerFactory>(),
            prefix
        ));

        services.Replace(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(PrefixedLogger<>)));

        return services;
    }
}
