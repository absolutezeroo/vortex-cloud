using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Vortex.Plugins.Exports;

internal sealed class ExportRegistry(ILogger? logger = null)
{
    private readonly ConcurrentDictionary<(Type t, string key), object> _map = new();
    private readonly ILogger? _logger = logger;

    public ReloadableExport<T> GetOrCreate<T>(string exportKey)
        where T : class =>
        (ReloadableExport<T>)
            _map.GetOrAdd((typeof(T), exportKey), _ => new ReloadableExport<T>(_logger));

    /// <summary>Publishes an export and disposes the instance it replaces, immediately.</summary>
    public Task SwapAsync<T>(string exportKey, T instance)
        where T : class => GetOrCreate<T>(exportKey).SwapAsync(instance);

    /// <summary>
    /// Publishes an export as part of a plugin activation, recording the displaced instance in
    /// <paramref name="journal" /> so the bind can be undone if a later activation step fails.
    /// </summary>
    public async Task SwapJournaledAsync<T>(string exportKey, T instance, ExportJournal journal)
        where T : class
    {
        IExportSlot slot = GetOrCreate<T>(exportKey);
        object? previous = await slot.SwapDeferredAsync(instance).ConfigureAwait(false);

        journal.Record(slot, exportKey, previous, instance);
    }
}
