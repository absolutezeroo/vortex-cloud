using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Crypto.Configuration;
using Vortex.Primitives.Crypto;

namespace Vortex.Crypto.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVortexCrypto(
        this IServiceCollection services,
        HostApplicationBuilder builder
    )
    {
        services.Configure<CryptoConfig>(
            builder.Configuration.GetSection(CryptoConfig.SECTION_NAME)
        );

        services.AddSingleton<IRsaService, RsaService>();
        services.AddSingleton<IDiffieService, DiffieService>();

        return services;
    }
}
