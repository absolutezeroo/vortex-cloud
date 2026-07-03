using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Turbo.Plugins.Exports;

internal sealed class ExportRegistry(ILogger? logger = null)
{
    private readonly ConcurrentDictionary<(Type t, string key), object> _map = new();
    private readonly ILogger? _logger = logger;

    public ReloadableExport<T> GetOrCreate<T>(string exportKey)
        where T : class =>
        (ReloadableExport<T>)
            _map.GetOrAdd((typeof(T), exportKey), _ => new ReloadableExport<T>(_logger));

    public Task SwapAsync<T>(string exportKey, T instance)
        where T : class => GetOrCreate<T>(exportKey).SwapAsync(instance);
}
