using System;
using System.Collections.Generic;
using Vortex.Primitives.Plugins;
using Vortex.Runtime.AssemblyProcessing;

namespace Vortex.Plugins;

internal sealed class PluginEnvelope : AssemblyDescriptor
{
    public required PluginManifest Manifest { get; init; }
    public required string Folder { get; init; }
    public required ITurboPlugin Instance { get; init; }
    public required IServiceProvider ServiceProvider { get; init; }
    public required List<object> Disposables { get; init; }
}
