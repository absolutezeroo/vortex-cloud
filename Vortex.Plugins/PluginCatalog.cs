using Vortex.Plugins.Exports;
using Vortex.Primitives.Plugins;
using Vortex.Primitives.Plugins.Exports;

namespace Vortex.Plugins;

internal sealed class PluginCatalog(ExportRegistry reg) : IPluginCatalog
{
    public IExport<T> GetExport<T>(string exportKey)
        where T : class => reg.GetOrCreate<T>(exportKey);
}
