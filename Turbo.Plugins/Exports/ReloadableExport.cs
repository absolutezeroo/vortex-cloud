using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Turbo.Primitives.Plugins.Exports;

namespace Turbo.Plugins.Exports;

public sealed class ReloadableExport<T>(ILogger? logger = null) : IExport<T>
    where T : class
{
    private readonly ILogger _logger = logger ?? NullLogger.Instance;
    private volatile T? _current;
    private ImmutableArray<Action<T>> _subs = [];

    public T Current =>
        _current ?? throw new InvalidOperationException($"Export {typeof(T).Name} not bound yet.");

    public async Task SwapAsync(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        T? previous = Interlocked.Exchange(ref _current, value);
        ImmutableArray<Action<T>> subs = _subs;

        try
        {
            if (previous is IAsyncDisposable a)
            {
                await a.DisposeAsync().ConfigureAwait(false);
            }
            else if (previous is IDisposable d)
            {
                d.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to dispose previous export instance for {ExportType}",
                typeof(T).Name
            );
        }

        foreach (Action<T> s in subs)
        {
            try
            {
                s(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Export swap subscriber failed for {ExportType}",
                    typeof(T).Name
                );
            }
        }
    }

    public IDisposable Subscribe(Action<T> onSwap)
    {
        ImmutableInterlocked.Update(ref _subs, a => a.Add(onSwap));

        T? current = _current;
        if (current is not null)
        {
            try
            {
                onSwap(current);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Export subscribe callback failed for {ExportType}",
                    typeof(T).Name
                );
            }
        }

        return new Unsub(() => ImmutableInterlocked.Update(ref _subs, a => a.Remove(onSwap)));
    }

    private sealed class Unsub(Action a) : IDisposable
    {
        private readonly Action _a = a;

        public void Dispose() => _a();
    }
}
