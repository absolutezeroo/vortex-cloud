using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Vortex.Runtime.AssemblyProcessing;

namespace Vortex.Messages;

/// <summary>
/// Scans this assembly's own <c>IMessageBehavior&lt;&gt;</c>/<c>IMessageHandler&lt;&gt;</c>
/// implementations (currently just <see cref="Behaviors.RateLimitBehavior"/>). Every other handler
/// lives in a plugin module assembly, which <c>PluginBootstrapper</c> scans automatically -
/// Vortex.Messages itself isn't a plugin module, so it needs this one small nudge. Registered
/// alongside PluginBootstrapper, before VortexEmulator starts the network listeners.
/// </summary>
public sealed class MessageBehaviorRegistrationService(
    AssemblyProcessor processor,
    IServiceProvider serviceProvider
) : IHostedService
{
    private IDisposable? _registration;

    public async Task StartAsync(CancellationToken ct)
    {
        _registration = await processor
            .ProcessAsync(Assembly.GetExecutingAssembly(), serviceProvider, ct)
            .ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken ct)
    {
        _registration?.Dispose();

        return Task.CompletedTask;
    }
}
