using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Turbo.Revisions.Configuration;
using RevisionType = Turbo.Revisions.Revision20260112.Revision20260112;

namespace Turbo.Revisions.Extensions;

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
