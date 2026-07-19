using System;

namespace Vortex.Primitives.Observability;

/// <summary>
/// Lifetime of an active trace context. Disposing restores the previously active context and ends
/// any associated logging scope and activity span.
/// </summary>
public interface ITurboTraceScope : IDisposable
{
    ITurboContext Context { get; }
}

/// <summary>
/// Ambient accessor for the current <see cref="ITurboContext"/>. Implementations are responsible
/// for propagating the correlation id across the async flow and across Orleans grain calls.
/// </summary>
public interface ITurboContextAccessor
{
    ITurboContext? Current { get; }

    /// <summary>
    /// Begins a new trace scope. When <paramref name="correlationId"/> is null a fresh id is
    /// generated (packet entry point); pass an existing id to continue an in-flight operation,
    /// for example when rehydrating the context on the grain side from the request context.
    /// </summary>
    ITurboTraceScope BeginScope(
        string operation,
        string? sessionKey = null,
        CorrelationId? correlationId = null,
        long? playerId = null,
        int? roomId = null
    );
}
