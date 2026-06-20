using System.Threading.Tasks;

namespace Turbo.Primitives.Plugins.Exports;

public interface IExportBinder
{
    Task ExportAsync<T>(string exportKey, T instance)
        where T : class;
}
