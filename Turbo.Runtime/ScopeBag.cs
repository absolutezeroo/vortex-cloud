using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Turbo.Runtime;

public sealed class ScopeBag : IAsyncDisposable
{
    private readonly ConcurrentDictionary<IServiceProvider, Lazy<AsyncScopeHolder>> _byOwner = new(
        concurrencyLevel: Environment.ProcessorCount,
        capacity: 4
    );

    public IServiceProvider Get(IServiceProvider owner)
    {
        var lazy = _byOwner.GetOrAdd(
            owner,
            static o => new Lazy<AsyncScopeHolder>(
                () => new AsyncScopeHolder { Scope = o.CreateAsyncScope() },
                LazyThreadSafetyMode.ExecutionAndPublication
            )
        );

        return lazy.Value.ServiceProvider;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var kv in _byOwner)
        {
            var lazy = kv.Value;
            if (!lazy.IsValueCreated)
                continue;

            try
            {
                await lazy.Value.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                // best-effort disposal; swallow
            }
        }

        _byOwner.Clear();
    }
}
