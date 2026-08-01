using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Messages.Configuration;
using Vortex.Messages.RateLimit;
using Vortex.Messages.Registry;
using Vortex.Pipeline;
using Vortex.Runtime.AssemblyProcessing;

namespace Vortex.Messages.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVortexMessageSystem(
        this IServiceCollection services,
        HostApplicationBuilder builder
    )
    {
        services.AddSingleton<IAssemblyFeatureProcessor, MessageFeatureProcessor>();
        services.AddSingleton<EnvelopeInvokerFactory<MessageContext>>();
        services.AddSingleton<MessageRegistry>();
        services.AddSingleton<MessageSystem>();

        services.Configure<RateLimitConfig>(
            builder.Configuration.GetSection(RateLimitConfig.SECTION_NAME)
        );
        services.AddSingleton<IRateLimiter, TokenBucketRateLimiter>();
        services.AddHostedService<MessageBehaviorRegistrationService>();

        return services;
    }
}
