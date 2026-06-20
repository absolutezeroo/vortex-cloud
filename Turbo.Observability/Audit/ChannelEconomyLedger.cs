using System;
using Microsoft.Extensions.Logging;
using Turbo.Observability.Diagnostics;
using Turbo.Primitives.Observability;

namespace Turbo.Observability.Audit;

/// <summary>
/// Channel-backed <see cref="IEconomyLedger"/>: stamps the ambient correlation id and capture time,
/// then enqueues for the shared durable writer. Non-blocking — no database access on the caller's
/// thread, so it never stalls a wallet operation.
/// </summary>
public sealed class ChannelEconomyLedger(
    AuditChannel channel,
    ITurboContextAccessor contextAccessor,
    ILogger<ChannelEconomyLedger> logger
) : IEconomyLedger
{
    private readonly AuditChannel _channel = channel;
    private readonly ITurboContextAccessor _contextAccessor = contextAccessor;
    private readonly ILogger<ChannelEconomyLedger> _logger = logger;

    public void Record(in EconomyLedgerEvent entry)
    {
        string? correlationId = _contextAccessor.Current?.CorrelationId.Value;
        LedgerRecord record = new LedgerRecord(entry, DateTime.UtcNow, correlationId);

        if (!_channel.TryWrite(record))
        {
            _logger.LogWarning(
                TurboEventIds.AuditDropped,
                "Economy ledger entry dropped (channel saturated): {Reason} {Delta} {Currency}",
                entry.Reason,
                entry.Delta,
                entry.Currency
            );
        }
    }
}
