using System;
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
using Vortex.Primitives.Plugins;
using Vortex.Runtime.AssemblyProcessing;
using Xunit;

namespace Vortex.Plugins.Tests;

/// <summary>
/// A plugin that shipped <c>Vortex.Primitives.dll</c> next to its own assembly used to be
/// byte-loaded whole: the plugin got its own <c>IVortexPlugin</c>, its own <c>IEventHandler&lt;&gt;</c>
/// and its own event records, none of which the host would ever match. The plugin simply did not
/// load, or worse, loaded with handlers that could never fire — and nothing said so. Packaging was
/// expected to avoid it and nothing enforced it.
/// </summary>
// Same collection as PluginActivationFailureTests: that class steers the shared test plugin through
// a process-wide environment variable, and a class running beside it would load a plugin told to
// fail. Grouping them makes the two run in sequence.
[Collection(TestPluginCollection.NAME)]
public sealed class PluginContractAssemblySharingTests : IDisposable
{
    private const string PLUGIN_KEY = "vortex-test-plugin";
    private const string ASSEMBLY_FILE = "Vortex.Plugins.TestPlugin.dll";
    private const string CONTRACT_FILE = "Vortex.Primitives.dll";

    private readonly string _pluginFolder;

    public PluginContractAssemblySharingTests()
    {
        _pluginFolder = CopyPluginWithContractAssembly();
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_pluginFolder, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp folder is not worth failing a test over.
        }
    }

    [Fact]
    public async Task APluginShippingAContractAssembly_StillLoads()
    {
        PluginManager manager = CreateManager();

        await manager.LoadAllAsync(unloadRemoved: false, CancellationToken.None);

        manager.GetLiveKeys().Should().Contain(PLUGIN_KEY);
    }

    [Fact]
    public void TheShippedContractAssembly_IsReportedAndNotLoadedFromTheFolder()
    {
        LoadedAssembly loaded = AssemblyMemoryLoader.LoadFromBytes(
            Path.Combine(_pluginFolder, ASSEMBLY_FILE)
        );

        loaded.ShadowedContractAssemblies.Should().Contain("Vortex.Primitives");

        // The type identity that matters: resolved through the plugin's context, IVortexPlugin is
        // still the host's. Any second copy makes every scan in the loader silently find nothing.
        Type contract = loaded
            .Assembly.GetTypes()
            .Single(t => typeof(IVortexPlugin).IsAssignableFrom(t) && !t.IsInterface)
            .GetInterfaces()
            .Single(i => i.Name == nameof(IVortexPlugin));

        contract.Should().BeSameAs(typeof(IVortexPlugin));
    }

    /// <summary>The mis-packaged shape: the plugin DLL plus a copy of a host contract assembly.</summary>
    private static string CopyPluginWithContractAssembly()
    {
        string source = AppContext.BaseDirectory;
        string destination = Path.Combine(
            Path.GetTempPath(),
            $"vortex-shadowed-plugin-{Guid.NewGuid():N}"
        );

        Directory.CreateDirectory(destination);

        foreach (string file in new[] { ASSEMBLY_FILE, CONTRACT_FILE })
        {
            File.Copy(Path.Combine(source, file), Path.Combine(destination, file), overwrite: true);
        }

        File.WriteAllText(
            Path.Combine(destination, "manifest.json"),
            JsonSerializer.Serialize(
                new
                {
                    Name = "Vortex Test Plugin",
                    Key = PLUGIN_KEY,
                    Version = "1.0.0",
                    Author = "Vortex",
                    AssemblyFile = ASSEMBLY_FILE,
                }
            )
        );

        return destination;
    }

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
}
