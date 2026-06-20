using Turbo.Plugins.Exports;
using Turbo.Primitives.Plugins;
using Turbo.Primitives.Plugins.Exports;

namespace Turbo.Plugins;

internal sealed class PluginCatalog(ExportRegistry reg) : IPluginCatalog
{
    public IExport<T> GetExport<T>(string exportKey)
        where T : class => reg.GetOrCreate<T>(exportKey);
}
