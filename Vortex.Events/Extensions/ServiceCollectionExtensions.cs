using Microsoft.Extensions.DependencyInjection;
using Vortex.Events.Registry;
using Vortex.Pipeline;
using Vortex.Primitives.Events;
using Vortex.Runtime.AssemblyProcessing;

namespace Vortex.Events.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVortexEventSystem(this IServiceCollection services)
    {
        services.AddSingleton<IAssemblyFeatureProcessor, EventFeatureProcessor>();
        services.AddSingleton<EnvelopeInvokerFactory<EventContext>>();
        services.AddSingleton<EventRegistry>();
        services.AddSingleton<EventSystem>();
        services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<EventSystem>());
        services.AddSingleton<ICancellableEventPublisher>(sp =>
            sp.GetRequiredService<EventSystem>()
        );

        return services;
    }
}
