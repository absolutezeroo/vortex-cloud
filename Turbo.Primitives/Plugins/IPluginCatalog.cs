using Turbo.Primitives.Plugins.Exports;

namespace Turbo.Primitives.Plugins;

public interface IPluginCatalog
{
    IExport<T> GetExport<T>(string exportKey)
        where T : class;
}
