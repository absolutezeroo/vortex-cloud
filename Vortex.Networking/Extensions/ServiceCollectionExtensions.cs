using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Networking.Configuration;
using Vortex.Networking.Revisions;
using Vortex.Networking.Session;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Networking.Revisions;

namespace Vortex.Networking.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTurboNetworking(
        this IServiceCollection services,
        HostApplicationBuilder builder
    )
    {
        services.Configure<NetworkingConfig>(
            builder.Configuration.GetSection(NetworkingConfig.SECTION_NAME)
        );

        services.AddSingleton<INetworkManager, NetworkManager>();
        services.AddSingleton<IRevisionManager, RevisionManager>();
        services.AddSingleton<ISessionGateway, SessionGateway>();

        return services;
    }
}
