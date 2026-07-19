using System;

namespace Vortex.Primitives.Observability;

/// <summary>
/// Stable identifier that ties together every log line, metric exemplar, trace span and audit
/// record produced while handling a single logical operation (typically one inbound packet).
/// Backed by a time-ordered GUID (v7) so raw values already sort chronologically.
/// </summary>
public readonly record struct CorrelationId(string Value)
{
    public static readonly CorrelationId None = new(string.Empty);

    public static CorrelationId New() => new(Guid.CreateVersion7().ToString("n"));

    public bool HasValue => !string.IsNullOrEmpty(Value);

    public override string ToString() => Value;
}
