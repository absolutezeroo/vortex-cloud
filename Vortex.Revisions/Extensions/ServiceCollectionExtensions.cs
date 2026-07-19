using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Revisions.Configuration;
using RevisionType = Vortex.Revisions.Revision20260701.Revision20260701;

namespace Vortex.Revisions.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTurboRevisions(
        this IServiceCollection services,
        HostApplicationBuilder builder
    )
    {
        services.Configure<ProtocolLimitsConfig>(
            builder.Configuration.GetSection(ProtocolLimitsConfig.SECTION_NAME)
        );

        services.AddSingleton<RevisionType>();

        return services;
    }
}
