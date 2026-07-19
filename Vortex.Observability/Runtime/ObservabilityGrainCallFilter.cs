using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Vortex.Observability.Context;
using Vortex.Observability.Diagnostics;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Runtime;

/// <summary>
/// Rehydrates the ambient <see cref="ITurboContext"/> on the grain side from the correlation id that
/// Orleans propagated through <see cref="RequestContext"/>. This lets business services invoked by a
/// grain observe the same correlation id without threading it through method signatures. The filter
/// is a no-op for calls that carry no correlation id (for example timer/reminder-triggered calls).
/// </summary>
public sealed class ObservabilityGrainCallFilter(
    ITurboContextAccessor accessor,
    ILogger<ObservabilityGrainCallFilter> logger
) : IIncomingGrainCallFilter
{
    private readonly ITurboContextAccessor _accessor = accessor;
    private readonly ILogger<ObservabilityGrainCallFilter> _logger = logger;

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (
            RequestContext.Get(TurboContextAccessor.RequestContextKey) is not string cid
            || cid.Length == 0
        )
        {
            await context.Invoke().ConfigureAwait(false);
            return;
        }

        ITurboTraceScope? scope = null;

        try
        {
            scope = _accessor.BeginScope(
                context.InterfaceMethod?.Name ?? "grain",
                correlationId: new CorrelationId(cid)
            );
        }
        catch (Exception ex)
        {
            // Never let observability break a grain call; degrade to no context.
            _logger.LogWarning(
                TurboEventIds.GrainContextFault,
                ex,
                "Failed to rehydrate trace context for grain call."
            );
        }

        try
        {
            await context.Invoke().ConfigureAwait(false);
        }
        finally
        {
            scope?.Dispose();
        }
    }
}
