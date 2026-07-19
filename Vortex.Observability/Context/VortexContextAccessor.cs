using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Vortex.Observability.Configuration;
using Vortex.Observability.Diagnostics;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Context;

/// <summary>
/// <see cref="AsyncLocal{T}"/>-backed implementation of <see cref="IVortexContextAccessor"/>.
/// On <see cref="BeginScope"/> it:
/// <list type="number">
/// <item>sets the ambient context for the current async flow;</item>
/// <item>writes the correlation id into Orleans <see cref="RequestContext"/> so it propagates across
/// grain calls;</item>
/// <item>opens a logging scope so every subsequent log line carries the id;</item>
/// <item>optionally starts an <see cref="Activity"/> span (OpenTelemetry-ready).</item>
/// </list>
/// Disposing the returned scope unwinds all four in reverse.
/// </summary>
public sealed class VortexContextAccessor : IVortexContextAccessor
{
    public const string RequestContextKey = "vortex-cid";

    private static readonly AsyncLocal<IVortexContext?> Ambient = new();

    private readonly ILogger _logger;
    private readonly bool _tracingEnabled;

    public VortexContextAccessor(
        ILoggerFactory loggerFactory,
        IOptions<ObservabilityConfig> options
    )
    {
        _logger = loggerFactory.CreateLogger("Vortex.Trace");
        _tracingEnabled = options.Value.TracingEnabled;
    }

    public IVortexContext? Current => Ambient.Value;

    public IVortexTraceScope BeginScope(
        string operation,
        string? sessionKey = null,
        CorrelationId? correlationId = null,
        long? playerId = null,
        int? roomId = null
    )
    {
        IVortexContext? previous = Ambient.Value;
        CorrelationId id = correlationId ?? CorrelationId.New();
        long? resolvedPlayerId = playerId ?? previous?.PlayerId;
        int? resolvedRoomId = roomId ?? previous?.RoomId;
        VortexContext context = new VortexContext(
            id,
            operation,
            sessionKey ?? previous?.SessionKey,
            resolvedPlayerId,
            resolvedRoomId
        );

        Ambient.Value = context;
        RequestContext.Set(RequestContextKey, id.Value);

        IDisposable? logScope = _logger.BeginScope(
            new Dictionary<string, object?>
            {
                ["CorrelationId"] = id.Value,
                ["Operation"] = operation,
            }
        );

        Activity? activity = _tracingEnabled
            ? VortexTelemetry.ActivitySource.StartActivity(operation)
            : null;
        activity?.SetTag("Vortex.correlation_id", id.Value);

        return new TraceScope(context, previous, logScope, activity);
    }

    private sealed class TraceScope(
        IVortexContext context,
        IVortexContext? previous,
        IDisposable? logScope,
        Activity? activity
    ) : IVortexTraceScope
    {
        public IVortexContext Context { get; } = context;

        public void Dispose()
        {
            activity?.Dispose();
            logScope?.Dispose();
            Ambient.Value = previous;
        }
    }
}
