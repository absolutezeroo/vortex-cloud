using System.Threading.Tasks;

namespace Vortex.Primitives.Plugins.Exports;

public interface IExportBinder
{
    Task ExportAsync<T>(string exportKey, T instance)
        where T : class;
}
