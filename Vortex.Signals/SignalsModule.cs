using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Primitives.Hosting;
using Vortex.Primitives.Plugins;
using Vortex.Primitives.Signals;
using Vortex.Runtime.AssemblyProcessing;

namespace Vortex.Signals;

/// <summary>
/// Registers the translation layer: the vocabulary, the interest gate, and the assembly processor
/// that discovers translators.
/// </summary>
/// <remarks>
/// The processor is registered as an <see cref="IAssemblyFeatureProcessor"/> beside the event one,
/// which is what makes a translator arrive by the same route as a handler — including from a plugin,
/// and including its removal on unload.
/// </remarks>
public sealed class SignalsModule : IHostPluginModule
{
    public string Key => "vortex-signals";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        services.AddSingleton<SignalVocabulary>();
        services.AddSingleton<ISignalVocabulary>(sp => sp.GetRequiredService<SignalVocabulary>());

        services.AddSingleton<ISignalInterest, SignalInterest>();

        // No consumer registers an interest source until stage 2 wires the reward-track one, and
        // with none registered every translator stops at the gate. That is the right answer rather
        // than a failure: a hotel with no progression content should pay nothing for signals.
        services.AddSingleton<IAssemblyFeatureProcessor, SignalTranslatorFeatureProcessor>();
    }
}
