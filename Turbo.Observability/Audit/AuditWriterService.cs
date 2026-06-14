using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Turbo.Database.Context;
using Turbo.Database.Entities.Audit;
using Turbo.Observability.Configuration;
using Turbo.Observability.Diagnostics;

namespace Turbo.Observability.Audit;

/// <summary>
/// Single background consumer of the durable-observability channel. It batches records of every family
/// (audit, economy ledger, item forensics), routes each to its table, and persists them with one
/// <c>SaveChanges</c> per batch — keeping all durable writes off the gameplay hot path. A failed flush
/// is logged and the batch is dropped (best-effort durability) rather than wedging the writer; a
/// dead-letter/retry strategy is a deliberate later refinement.
/// </summary>
public sealed class AuditWriterService(
    AuditChannel channel,
    IDbContextFactory<TurboDbContext> dbContextFactory,
    IOptions<ObservabilityConfig> options,
    ILogger<AuditWriterService> logger
) : BackgroundService
{
    private readonly AuditChannel _channel = channel;
    private readonly IDbContextFactory<TurboDbContext> _dbContextFactory = dbContextFactory;
    private readonly int _batchSize = Math.Max(1, options.Value.AuditBatchSize);
    private readonly ILogger<AuditWriterService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = _channel.Reader;
        var batch = new List<DurableRecord>(_batchSize);

        try
        {
            while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                while (batch.Count < _batchSize && reader.TryRead(out var record))
                    batch.Add(record);

                if (batch.Count > 0)
                {
                    await FlushAsync(batch, stoppingToken).ConfigureAwait(false);
                    batch.Clear();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested; fall through to a best-effort final drain.
        }

        DrainRemaining(reader, batch);
    }

    private async Task FlushAsync(List<DurableRecord> batch, CancellationToken ct)
    {
        var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            foreach (var record in batch)
                db.Add(MapEntity(record));

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                TurboEventIds.AuditWriteFailed,
                ex,
                "Failed to persist a batch of {Count} durable observability record(s); batch discarded.",
                batch.Count
            );
        }
        finally
        {
            await db.DisposeAsync().ConfigureAwait(false);
        }
    }

    private void DrainRemaining(ChannelReader<DurableRecord> reader, List<DurableRecord> batch)
    {
        batch.Clear();

        while (reader.TryRead(out var record))
            batch.Add(record);

        if (batch.Count == 0)
            return;

        try
        {
            using var db = _dbContextFactory.CreateDbContext();

            foreach (var record in batch)
                db.Add(MapEntity(record));

            db.SaveChanges();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                TurboEventIds.AuditWriteFailed,
                ex,
                "Failed to persist {Count} durable observability record(s) during shutdown drain.",
                batch.Count
            );
        }
    }

    private static object MapEntity(DurableRecord record) =>
        record switch
        {
            AuditRecord audit => MapAudit(audit),
            LedgerRecord ledger => MapLedger(ledger),
            ItemRecord item => MapItem(item),
            _ => throw new ArgumentOutOfRangeException(
                nameof(record),
                record.GetType().Name,
                "Unknown durable observability record type."
            ),
        };

    private static AuditEventEntity MapAudit(AuditRecord record) =>
        new()
        {
            OccurredAt = record.OccurredAt,
            Category = record.Event.Category,
            Action = record.Event.Action,
            Severity = record.Event.Severity,
            Result = record.Event.Result,
            CorrelationId = record.CorrelationId,
            ActorPlayerId = record.Event.ActorPlayerId,
            TargetPlayerId = record.Event.TargetPlayerId,
            RoomId = record.Event.RoomId,
            ItemId = record.Event.ItemId,
            Data = record.Event.Data,
        };

    private static EconomyLedgerEntity MapLedger(LedgerRecord record) =>
        new()
        {
            OccurredAt = record.OccurredAt,
            PlayerId = record.Event.PlayerId,
            Currency = record.Event.Currency,
            ActivityPointType = record.Event.ActivityPointType,
            Delta = record.Event.Delta,
            BalanceAfter = record.Event.BalanceAfter,
            Reason = record.Event.Reason,
            CorrelationId = record.CorrelationId,
            RefId = record.Event.RefId,
        };

    private static ItemEventEntity MapItem(ItemRecord record) =>
        new()
        {
            OccurredAt = record.OccurredAt,
            ItemId = record.Event.ItemId,
            EventType = record.Event.EventType,
            ActorPlayerId = record.Event.ActorPlayerId,
            FromOwnerId = record.Event.FromOwnerId,
            ToOwnerId = record.Event.ToOwnerId,
            RoomId = record.Event.RoomId,
            CorrelationId = record.CorrelationId,
            Data = record.Event.Data,
        };
}
