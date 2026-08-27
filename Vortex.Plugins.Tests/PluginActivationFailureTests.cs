using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vortex.Plugins.Configuration;
using Vortex.Runtime.AssemblyProcessing;
using Xunit;

namespace Vortex.Plugins.Tests;

/// <summary>
/// Drives the real <see cref="PluginManager" /> against a real, byte-loaded plugin assembly
/// (<c>Vortex.Plugins.TestPlugin</c>) copied into a temporary folder, so the activation path under
/// test is the production one — assembly load context and all.
/// <para>
/// The plugin is told where to fail through an environment variable, because it runs in its own
/// collectible <c>AssemblyLoadContext</c> and therefore does not share statics with the test host.
/// It writes a breadcrumb trail to a second environment variable, which is how these tests observe
/// what the rollback actually stopped and disposed.
/// </para>
/// </summary>
[Collection(TestPluginCollection.NAME)]
public sealed class PluginActivationFailureTests : IDisposable
{
    private const string PLUGIN_KEY = "vortex-test-plugin";
    private const string FAILURE_VARIABLE = "VORTEX_TEST_PLUGIN_FAILURE";
    private const string TRACE_VARIABLE = "VORTEX_TEST_PLUGIN_TRACE";

    private readonly string _pluginFolder;

    public PluginActivationFailureTests()
    {
        _pluginFolder = CopyTestPluginToTempFolder();
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(FAILURE_VARIABLE, null);
        Environment.SetEnvironmentVariable(TRACE_VARIABLE, null);

        try
        {
            Directory.Delete(_pluginFolder, recursive: true);
        }
        catch (IOException)
        {
            // The ALC may still hold nothing (we byte-load), but a virus scanner can briefly lock a
            // file. A leftover temp folder is not worth failing a test over.
        }
    }

    // ── The activation succeeds ──────────────────────────────────────────────

    [Fact]
    public async Task AHealthyPlugin_BecomesLive()
    {
        PluginManager manager = CreateManager();

        await manager.LoadAllAsync(unloadRemoved: false, CancellationToken.None);

        Live(manager).Should().Contain(PLUGIN_KEY);
        Trace().Should().Contain("plugin-start");
    }

    // ── Each failure point leaves nothing behind ─────────────────────────────

    [Theory]
    [InlineData("Migration")]
    [InlineData("BindExports")]
    [InlineData("HostedServiceStart")]
    [InlineData("PluginStart")]
    public async Task AFailedActivation_NeverAppearsAsLive(string failurePoint)
    {
        Environment.SetEnvironmentVariable(FAILURE_VARIABLE, failurePoint);

        PluginManager manager = CreateManager();

        await manager.LoadAllAsync(unloadRemoved: false, CancellationToken.None);

        Live(manager)
            .Should()
            .BeEmpty($"an activation that failed at {failurePoint} must not be published");
    }

    [Theory]
    [InlineData("BindExports")]
    [InlineData("HostedServiceStart")]
    [InlineData("PluginStart")]
    public async Task AFailedActivation_DisposesTheServiceProvider(string failurePoint)
    {
        Environment.SetEnvironmentVariable(FAILURE_VARIABLE, failurePoint);

        PluginManager manager = CreateManager();

        await manager.LoadAllAsync(unloadRemoved: false, CancellationToken.None);

        // TrackedResource is a singleton in the plugin's container; its disposal only happens when
        // the container itself is disposed.
        Trace()
            .Should()
            .Contain(
                "resource-dispose",
                "the plugin's service provider must be disposed when its activation fails"
            );
    }

    [Fact]
    public async Task WhenTheSecondHostedServiceFails_TheFirstOneIsStopped()
    {
        Environment.SetEnvironmentVariable(FAILURE_VARIABLE, "HostedServiceStart");

        PluginManager manager = CreateManager();

        await manager.LoadAllAsync(unloadRemoved: false, CancellationToken.None);

        List<string> trace = Trace();

        trace.Should().Contain("start-first");
        trace
            .Should()
            .Contain(
                "stop-first",
                "the rollback must stop the services that did start, not just abandon them"
            );
    }

    [Fact]
    public async Task WhenThePluginStartFails_EveryHostedServiceIsStopped()
    {
        Environment.SetEnvironmentVariable(FAILURE_VARIABLE, "PluginStart");

        PluginManager manager = CreateManager();

        await manager.LoadAllAsync(unloadRemoved: false, CancellationToken.None);

        List<string> trace = Trace();

        trace.Should().Contain("start-first");
        trace.Should().Contain("start-second");
        trace.Should().Contain("stop-first");
        trace.Should().Contain("stop-second");
    }

    [Fact]
    public async Task AFailedActivation_DisposesThePluginInstance()
    {
        Environment.SetEnvironmentVariable(FAILURE_VARIABLE, "PluginStart");

        PluginManager manager = CreateManager();

        await manager.LoadAllAsync(unloadRemoved: false, CancellationToken.None);

        // IVortexPlugin is IAsyncDisposable, but the instance is Activator-created rather than
        // DI-owned, so nothing used to dispose it at all.
        Trace().Should().Contain("plugin-dispose");
    }

    [Fact]
    public async Task AMigrationFailure_StartsNoHostedService()
    {
        Environment.SetEnvironmentVariable(FAILURE_VARIABLE, "Migration");

        PluginManager manager = CreateManager();

        await manager.LoadAllAsync(unloadRemoved: false, CancellationToken.None);

        List<string> trace = Trace();

        trace.Should().Contain("migrate");
        trace.Should().NotContain("start-first", "migrations run before any hosted service");
        trace.Should().NotContain("plugin-start");
    }

    [Fact]
    public async Task ARollbackThatCannotStopAService_StillDisposesTheContainer()
    {
        // The second hosted service throws on start; the first one's StopAsync runs during rollback.
        // Even if a compensation misbehaves, the later (earlier-pushed) steps must still run.
        Environment.SetEnvironmentVariable(FAILURE_VARIABLE, "HostedServiceStart");

        PluginManager manager = CreateManager();

        await manager.LoadAllAsync(unloadRemoved: false, CancellationToken.None);

        List<string> trace = Trace();

        trace.Should().Contain("stop-first");
        trace.Should().Contain("resource-dispose");
        Live(manager).Should().BeEmpty();
    }

    // ── Recovery ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AfterAFailedActivation_TheSamePluginCanLoadCleanly()
    {
        Environment.SetEnvironmentVariable(FAILURE_VARIABLE, "PluginStart");

        PluginManager manager = CreateManager();
        await manager.LoadAllAsync(unloadRemoved: false, CancellationToken.None);

        Live(manager).Should().BeEmpty();

        // Whatever was wrong is fixed; the next load must succeed rather than trip over leftovers.
        Environment.SetEnvironmentVariable(FAILURE_VARIABLE, null);
        Environment.SetEnvironmentVariable(TRACE_VARIABLE, null);

        await manager.LoadAllAsync(unloadRemoved: false, CancellationToken.None);

        Live(manager).Should().Contain(PLUGIN_KEY);
        Trace().Should().Contain("plugin-start");
    }

    [Fact]
    public async Task ReloadingOverAHealthyPlugin_LeavesExactlyOneLiveActivation()
    {
        PluginManager manager = CreateManager();

        await manager.LoadAllAsync(unloadRemoved: false, CancellationToken.None);
        await manager.LoadAllAsync(unloadRemoved: false, CancellationToken.None);

        Live(manager).Should().ContainSingle().Which.Should().Be(PLUGIN_KEY);
    }

    [Fact]
    public async Task AReloadThatFails_DoesNotLeaveTheTornDownPluginLive()
    {
        PluginManager manager = CreateManager();

        await manager.LoadAllAsync(unloadRemoved: false, CancellationToken.None);
        Live(manager).Should().Contain(PLUGIN_KEY);

        // The replacement activation fails after the previous one has already been torn down.
        Environment.SetEnvironmentVariable(FAILURE_VARIABLE, "PluginStart");

        await manager.LoadAllAsync(unloadRemoved: false, CancellationToken.None);

        Live(manager)
            .Should()
            .BeEmpty(
                "the old envelope is already stopped and unloaded, so leaving it in _live would "
                    + "advertise a dead plugin and tear it down a second time"
            );
    }

    // ── Harness ──────────────────────────────────────────────────────────────

    private PluginManager CreateManager()
    {
        ServiceCollection hostServices = new();

        PluginConfig config = new()
        {
            PluginFolderPath = Path.Combine(Path.GetTempPath(), $"vortex-empty-{Guid.NewGuid():N}"),
            DevPluginPaths = [_pluginFolder],
            HotReloadEnabled = false,
        };

        return new PluginManager(
            hostServices.BuildServiceProvider(),
            new AssemblyProcessor([]),
            Options.Create(config),
            NullLogger<PluginManager>.Instance
        );
    }

    private static List<string> Live(PluginManager manager) => manager.GetLiveKeys().ToList();

    private static List<string> Trace() =>
        (Environment.GetEnvironmentVariable(TRACE_VARIABLE) ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

    /// <summary>
    /// Copies the built test-plugin assembly into a scratch folder with a manifest, which is exactly
    /// the shape <see cref="PluginManager" /> discovers on disk.
    /// <para>
    /// Only the plugin's own DLL is copied, deliberately. <c>ByteLoadingAlc</c> byte-loads every
    /// assembly it finds in the plugin folder, so shipping <c>Vortex.Primitives.dll</c> alongside it
    /// would give the plugin its own <c>IVortexPlugin</c> type, distinct from the host's — and
    /// <c>AssemblyExplorer.FindType</c> would then find nothing. Leaving the shared contracts out
    /// makes them resolve from the default load context, which is how real plugins are packaged.
    /// </para>
    /// </summary>
    private static string CopyTestPluginToTempFolder()
    {
        string source = AppContext.BaseDirectory;
        string destination = Path.Combine(
            Path.GetTempPath(),
            $"vortex-test-plugin-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(destination);

        const string ASSEMBLY_FILE = "Vortex.Plugins.TestPlugin.dll";

        File.Copy(
            Path.Combine(source, ASSEMBLY_FILE),
            Path.Combine(destination, ASSEMBLY_FILE),
            overwrite: true
        );

        File.WriteAllText(
            Path.Combine(destination, "manifest.json"),
            JsonSerializer.Serialize(
                new
                {
                    Name = "Vortex Test Plugin",
                    Key = PLUGIN_KEY,
                    Version = "1.0.0",
                    Author = "Vortex",
                    AssemblyFile = "Vortex.Plugins.TestPlugin.dll",
                }
            )
        );

        return destination;
    }
}
