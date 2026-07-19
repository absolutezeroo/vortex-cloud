using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Plugins.Configuration;
using Vortex.Primitives.Plugins;

namespace Vortex.Plugins.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTurboPlugins(
        this IServiceCollection services,
        HostApplicationBuilder builder
    )
    {
        IConfigurationSection pluginSection = builder.Configuration.GetSection(
            PluginConfig.SECTION_NAME
        );

        services.Configure<PluginConfig>(pluginSection);

        services.AddSingleton<PluginManager>();
        services.AddHostedService<PluginBootstrapper>();

        if (builder.Environment.IsDevelopment() && pluginSection.GetValue<bool>("HotReloadEnabled"))
        {
            services.AddHostedService<PluginHotReloadService>();
        }

        return services;
    }

    public static IServiceCollection AddHostPlugin<TModule>(
        this IServiceCollection services,
        HostApplicationBuilder builder
    )
        where TModule : class, IHostPluginModule, new()
    {
        TModule module = new TModule();

        module.ConfigureServices(services, builder);

        services.AddSingleton<IHostPluginModule>(module);

        return services;
    }
}
