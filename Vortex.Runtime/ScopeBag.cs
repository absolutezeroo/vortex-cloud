using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Vortex.Runtime;

public sealed class ScopeBag : IAsyncDisposable
{
    private readonly ConcurrentDictionary<IServiceProvider, Lazy<AsyncScopeHolder>> _byOwner = new(
        concurrencyLevel: Environment.ProcessorCount,
        capacity: 4
    );

    public IServiceProvider Get(IServiceProvider owner)
    {
        Lazy<AsyncScopeHolder> lazy = _byOwner.GetOrAdd(
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
        foreach (KeyValuePair<IServiceProvider, Lazy<AsyncScopeHolder>> kv in _byOwner)
        {
            Lazy<AsyncScopeHolder> lazy = kv.Value;
            if (!lazy.IsValueCreated)
            {
                continue;
            }

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
