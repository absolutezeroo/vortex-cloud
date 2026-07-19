using System.Threading.Tasks;
using Vortex.Primitives.Plugins.Exports;

namespace Vortex.Plugins.Exports;

internal sealed class ExportBinder(ExportRegistry registry) : IExportBinder
{
    public Task ExportAsync<T>(string exportKey, T instance)
        where T : class => registry.SwapAsync(exportKey, instance);
}
