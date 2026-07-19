using System;
using Microsoft.Extensions.Logging;
using Vortex.Observability.Diagnostics;
using Vortex.Primitives.Observability;

namespace Vortex.Observability.Audit;

/// <summary>
/// Channel-backed <see cref="IItemForensics"/>: stamps the ambient correlation id and capture time,
/// then enqueues for the shared durable writer. Non-blocking — no database access on the caller's
/// thread.
/// </summary>
public sealed class ChannelItemForensics(
    AuditChannel channel,
    IVortexContextAccessor contextAccessor,
    ILogger<ChannelItemForensics> logger
) : IItemForensics
{
    private readonly AuditChannel _channel = channel;
    private readonly IVortexContextAccessor _contextAccessor = contextAccessor;
    private readonly ILogger<ChannelItemForensics> _logger = logger;

    public void Record(in ItemForensicEvent itemEvent)
    {
        string? correlationId = _contextAccessor.Current?.CorrelationId.Value;
        ItemRecord record = new ItemRecord(itemEvent, DateTime.UtcNow, correlationId);

        if (!_channel.TryWrite(record))
        {
            _logger.LogWarning(
                VortexEventIds.AuditDropped,
                "Item forensic event dropped (channel saturated): {EventType} item {ItemId}",
                itemEvent.EventType,
                itemEvent.ItemId
            );
        }
    }
}
