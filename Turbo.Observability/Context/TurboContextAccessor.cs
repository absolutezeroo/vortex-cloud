using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Turbo.Observability.Configuration;
using Turbo.Observability.Diagnostics;
using Turbo.Primitives.Observability;

namespace Turbo.Observability.Context;

/// <summary>
/// <see cref="AsyncLocal{T}"/>-backed implementation of <see cref="ITurboContextAccessor"/>.
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
public sealed class TurboContextAccessor : ITurboContextAccessor
{
    public const string RequestContextKey = "turbo-cid";

    private static readonly AsyncLocal<ITurboContext?> Ambient = new();

    private readonly ILogger _logger;
    private readonly bool _tracingEnabled;

    public TurboContextAccessor(ILoggerFactory loggerFactory, IOptions<ObservabilityConfig> options)
    {
        _logger = loggerFactory.CreateLogger("Turbo.Trace");
        _tracingEnabled = options.Value.TracingEnabled;
    }

    public ITurboContext? Current => Ambient.Value;

    public ITurboTraceScope BeginScope(
        string operation,
        string? sessionKey = null,
        CorrelationId? correlationId = null
    )
    {
        var previous = Ambient.Value;
        var id = correlationId ?? CorrelationId.New();
        var context = new TurboContext(
            id,
            operation,
            sessionKey ?? previous?.SessionKey,
            previous?.PlayerId,
            previous?.RoomId
        );

        Ambient.Value = context;
        RequestContext.Set(RequestContextKey, id.Value);

        var logScope = _logger.BeginScope(
            new Dictionary<string, object?>
            {
                ["CorrelationId"] = id.Value,
                ["Operation"] = operation,
            }
        );

        Activity? activity = _tracingEnabled
            ? TurboTelemetry.ActivitySource.StartActivity(operation)
            : null;
        activity?.SetTag("turbo.correlation_id", id.Value);

        return new TraceScope(context, previous, logScope, activity);
    }

    private sealed class TraceScope(
        ITurboContext context,
        ITurboContext? previous,
        IDisposable? logScope,
        Activity? activity
    ) : ITurboTraceScope
    {
        public ITurboContext Context { get; } = context;

        public void Dispose()
        {
            activity?.Dispose();
            logScope?.Dispose();
            Ambient.Value = previous;
        }
    }
}
