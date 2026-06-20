using System;
using Microsoft.Extensions.Logging;
using Turbo.Observability.Diagnostics;
using Turbo.Primitives.Observability;

namespace Turbo.Observability.Audit;

/// <summary>
/// Channel-backed <see cref="IPerformanceLogSink"/>: stamps the ambient correlation id and capture time,
/// then enqueues for the shared durable writer. Non-blocking � no database access on the caller's
/// thread.
/// </summary>
public sealed class ChannelPerformanceLogSink(
    AuditChannel channel,
    ITurboContextAccessor contextAccessor,
    ILogger<ChannelPerformanceLogSink> logger
) : IPerformanceLogSink
{
    private readonly AuditChannel _channel = channel;
    private readonly ITurboContextAccessor _contextAccessor = contextAccessor;
    private readonly ILogger<ChannelPerformanceLogSink> _logger = logger;

    public void Record(in PerformanceLogEvent entry)
    {
        string? correlationId = _contextAccessor.Current?.CorrelationId.Value;
        PerformanceLogRecord record = new PerformanceLogRecord(entry, DateTime.UtcNow, correlationId);

        if (!_channel.TryWrite(record))
        {
            _logger.LogWarning(
                TurboEventIds.AuditDropped,
                "Performance log dropped (channel saturated): {ElapsedTime}ms user-agent {UserAgent}",
                entry.ElapsedTime,
                entry.UserAgent
            );
        }
    }
}
