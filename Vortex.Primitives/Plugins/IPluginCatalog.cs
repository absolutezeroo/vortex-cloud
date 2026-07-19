using Vortex.Primitives.Plugins.Exports;

namespace Vortex.Primitives.Plugins;

public interface IPluginCatalog
{
    IExport<T> GetExport<T>(string exportKey)
        where T : class;
}
