using Xunit;

namespace Vortex.Plugins.Tests;

/// <summary>
/// Every test class that loads <c>Vortex.Plugins.TestPlugin</c> belongs here. The plugin is told
/// where to fail through a process-wide environment variable — it byte-loads into its own
/// collectible context and shares no statics with the host — so two such classes running in
/// parallel steer each other's plugin.
/// </summary>
[CollectionDefinition(NAME, DisableParallelization = true)]
public sealed class TestPluginCollection
{
    public const string NAME = "vortex-test-plugin";
}
