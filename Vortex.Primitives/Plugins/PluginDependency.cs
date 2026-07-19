namespace Vortex.Primitives.Plugins;

public sealed record PluginDependency(string Key, string? MinVersion = null);
