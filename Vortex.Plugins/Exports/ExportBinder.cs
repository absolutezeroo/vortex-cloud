using System.Threading.Tasks;
using Vortex.Primitives.Plugins.Exports;

namespace Vortex.Plugins.Exports;

/// <summary>
/// The <see cref="IExportBinder" /> handed to a plugin during activation. Every bind is journaled so
/// a failure later in the activation can put the previous exports back.
/// </summary>
internal sealed class ExportBinder(ExportRegistry registry, ExportJournal journal) : IExportBinder
{
    public Task ExportAsync<T>(string exportKey, T instance)
        where T : class => registry.SwapJournaledAsync(exportKey, instance, journal);
}
