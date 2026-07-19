using System;
using Microsoft.Extensions.Logging;
using Vortex.Observability.Diagnostics;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Audit;

/// <summary>
/// Channel-backed <see cref="IAuditSink"/>: enriches the event with the ambient correlation id and a
/// capture timestamp, then enqueues it for the background writer. Strictly non-blocking — it performs
/// no database access on the caller's thread, honouring the contract that audit never stalls gameplay.
/// </summary>
public sealed class ChannelAuditSink(
    AuditChannel channel,
    ITurboContextAccessor contextAccessor,
    ILogger<ChannelAuditSink> logger
) : IAuditSink
{
    private readonly AuditChannel _channel = channel;
    private readonly ITurboContextAccessor _contextAccessor = contextAccessor;
    private readonly ILogger<ChannelAuditSink> _logger = logger;

    public void Emit(in AuditEvent auditEvent)
    {
        string? correlationId = auditEvent.CorrelationId.HasValue
            ? auditEvent.CorrelationId.Value
            : _contextAccessor.Current?.CorrelationId.Value;

        AuditRecord record = new AuditRecord(auditEvent, DateTime.UtcNow, correlationId);

        if (!_channel.TryWrite(record))
        {
            // Never block the caller; a saturated channel means the writer cannot keep up.
            _logger.LogWarning(
                TurboEventIds.AuditDropped,
                "Audit event dropped (channel saturated): {Category}/{Action}",
                auditEvent.Category,
                auditEvent.Action
            );
        }
    }
}
