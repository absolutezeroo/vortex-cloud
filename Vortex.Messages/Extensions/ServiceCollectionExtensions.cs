using Microsoft.Extensions.DependencyInjection;
using Vortex.Messages.Registry;
using Vortex.Pipeline;
using Vortex.Runtime.AssemblyProcessing;

namespace Vortex.Messages.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTurboMessageSystem(this IServiceCollection services)
    {
        services.AddSingleton<IAssemblyFeatureProcessor, MessageFeatureProcessor>();
        services.AddSingleton<EnvelopeInvokerFactory<MessageContext>>();
        services.AddSingleton<MessageRegistry>();
        services.AddSingleton<MessageSystem>();

        return services;
    }
}
