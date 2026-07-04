using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;
using Turbo.Main.Configuration;
using Turbo.Primitives.Orleans;

namespace Turbo.Main.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static HostApplicationBuilder AddOrleans(this HostApplicationBuilder builder)
    {
        OrleansHostConfig hostConfig =
            builder
                .Configuration.GetSection(OrleansHostConfig.SECTION_NAME)
                .Get<OrleansHostConfig>()
            ?? new OrleansHostConfig();

        if (!builder.Environment.IsDevelopment())
        {
            System.Console.Error.WriteLine(
                "WARNING: Orleans is running with single-node localhost clustering and in-memory "
                    + "grain storage/streams outside Development. State does not survive a restart "
                    + "and this cannot scale beyond one silo. Configure a persistent clustering/"
                    + "storage provider before relying on this in production."
            );
        }

        builder.UseOrleans(
            (System.Action<ISiloBuilder>)(
                silo =>
                {
                    silo.Configure<GrainCollectionOptions>(options =>
                    {
                        options.CollectionAge = TimeSpan.FromMinutes(2);
                    });
                    silo.ConfigureEndpoints(
                        hostConfig.AdvertisedIp,
                        siloPort: hostConfig.SiloPort,
                        gatewayPort: hostConfig.GatewayPort,
                        listenOnAnyHostAddress: true
                    );

                    silo.UseLocalhostClustering()
                        .AddMemoryGrainStorage(OrleansStorageNames.PUB_SUB_STORE)
                        .AddMemoryGrainStorage(OrleansStorageNames.PLAYER_STORE)
                        .AddMemoryGrainStorage(OrleansStorageNames.ROOM_STORE)
                        .AddMemoryStreams(OrleansStreamProviders.DEFAULT_STREAM_PROVIDER)
                        .AddMemoryStreams(OrleansStreamProviders.ROOM_STREAM_PROVIDER);
                }
            )
        );

        return builder;
    }
}
