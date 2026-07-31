using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vortex.Primitives.Plugins;
using Vortex.Primitives.Plugins.Exports;

namespace Vortex.Plugins.TestPlugin;

/// <summary>Something for the plugin to export, so export restoration can be observed.</summary>
public interface ITestGreeter
{
    string Greet();
}

/// <summary>
/// The one <see cref="IVortexPlugin" /> in this assembly. It fails at whichever activation step
/// <see cref="FailureSwitch.Current" /> names, and traces the steps it reaches so the tests can
/// assert that a rollback stopped exactly what had been started.
/// </summary>
public sealed class TestPlugin : IVortexPlugin
{
    public const string KEY = "vortex-test-plugin";

    public string Key => KEY;

    public string Version => "1.0.0";

    public void ConfigureServices(IServiceCollection services, PluginManifest manifest)
    {
        services.AddSingleton<TrackedResource>();
        services.AddSingleton<IPluginDbModule, TestDbModule>();

        // Two hosted services: the tests need "the first started, the second failed" to prove the
        // rollback stops the first one rather than leaving it running.
        services.AddSingleton<IHostedService>(
            new TrackedHostedService("first", failOnStart: false)
        );
        services.AddSingleton<IHostedService>(
            new TrackedHostedService(
                "second",
                failOnStart: FailureSwitch.Current == FailurePoint.HostedServiceStart
            )
        );
    }

    public Task BindExportsAsync(IExportBinder binder, IServiceProvider services)
    {
        FailureSwitch.Trace("bind-exports");

        // Resolved (not merely registered) so the container actually constructs it: a
        // ServiceProvider only disposes the singletons it created, so an unresolved registration
        // would make "was the container disposed?" unobservable. This is the first activation step,
        // so every failure point downstream has a live singleton to account for.
        _ = services.GetRequiredService<TrackedResource>();

        if (FailureSwitch.Current == FailurePoint.BindExports)
        {
            throw new InvalidOperationException("Export binding failed on purpose.");
        }

        return binder.ExportAsync<ITestGreeter>("greeter", new PluginGreeter());
    }

    public Task StartAsync(IServiceProvider services, CancellationToken ct)
    {
        FailureSwitch.Trace("plugin-start");

        if (FailureSwitch.Current == FailurePoint.PluginStart)
        {
            throw new InvalidOperationException("Plugin start failed on purpose.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        FailureSwitch.Trace("plugin-stop");

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        FailureSwitch.Trace("plugin-dispose");

        return ValueTask.CompletedTask;
    }

    private sealed class PluginGreeter : ITestGreeter
    {
        public string Greet() => "from-plugin";
    }
}

/// <summary>A singleton whose disposal proves the plugin's service provider was disposed.</summary>
public sealed class TrackedResource : IDisposable
{
    public void Dispose() => FailureSwitch.Trace("resource-dispose");
}

/// <summary>Traces its own start/stop so rollback ordering is observable.</summary>
public sealed class TrackedHostedService(string name, bool failOnStart) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (failOnStart)
        {
            throw new InvalidOperationException($"Hosted service '{name}' failed on purpose.");
        }

        FailureSwitch.Trace($"start-{name}");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        FailureSwitch.Trace($"stop-{name}");

        return Task.CompletedTask;
    }
}

/// <summary>Runs before any hosted service, so a migration failure aborts the activation early.</summary>
public sealed class TestDbModule : IPluginDbModule
{
    public Task MigrateAsync(IServiceProvider sp, CancellationToken ct)
    {
        FailureSwitch.Trace("migrate");

        if (FailureSwitch.Current == FailurePoint.Migration)
        {
            throw new InvalidOperationException("Migration failed on purpose.");
        }

        return Task.CompletedTask;
    }

    public Task UninstallAsync(IServiceProvider sp, CancellationToken ct) => Task.CompletedTask;
}
