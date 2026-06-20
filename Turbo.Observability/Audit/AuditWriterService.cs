using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Turbo.Database.Context;
using Turbo.Database.Entities.Audit;
using Turbo.Database.Entities.Tracking;
using Turbo.Observability.Configuration;
using Turbo.Observability.Diagnostics;

namespace Turbo.Observability.Audit;

/// <summary>
///     Single background consumer of the durable-observability channel. It batches records of every family
///     (audit, economy ledger, item forensics, performance logs), routes each to its table, and persists them with one
///     <c>SaveChanges</c> per batch — keeping all durable writes off the gameplay hot path. A failed
///     flush retries a bounded number of times and then writes to a dead-letter file to avoid losing audit
///     history.
/// </summary>
public sealed class AuditWriterService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly int _batchSize;

    private readonly AuditChannel _channel;
    private readonly IDbContextFactory<TurboDbContext> _dbContextFactory;
    private readonly string _deadLetterPath;
    private readonly ILogger<AuditWriterService> _logger;
    private readonly int _retryAttempts;
    private readonly int _retryDelayMs;

    public AuditWriterService(
        AuditChannel channel,
        IDbContextFactory<TurboDbContext> dbContextFactory,
        IOptions<ObservabilityConfig> options,
        ILogger<AuditWriterService> logger
    )
    {
        _channel = channel;
        _dbContextFactory = dbContextFactory;
        _batchSize = Math.Max(1, options.Value.AuditBatchSize);
        _retryAttempts = Math.Max(0, options.Value.AuditWriteRetryAttempts);
        _retryDelayMs = Math.Max(0, options.Value.AuditWriteRetryDelayMs);

        string configuredPath = options.Value.AuditDeadLetterPath;
        _deadLetterPath =
            string.IsNullOrWhiteSpace(configuredPath) ? string.Empty
            : Path.IsPathRooted(configuredPath) ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ChannelReader<DurableRecord> reader = _channel.Reader;
        List<DurableRecord> batch = new(_batchSize);

        try
        {
            while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                while (batch.Count < _batchSize && reader.TryRead(out DurableRecord? record))
                {
                    batch.Add(record);
                }

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

        await DrainRemainingAsync(reader, batch).ConfigureAwait(false);
    }

    private async Task FlushAsync(List<DurableRecord> batch, CancellationToken ct)
    {
        if (batch.Count == 0)
        {
            return;
        }

        bool persisted = await TryPersistBatchWithRetryAsync(batch, ct).ConfigureAwait(false);
        if (persisted)
        {
            return;
        }

        await WriteDeadLetterAsync(batch).ConfigureAwait(false);
    }

    private async Task DrainRemainingAsync(
        ChannelReader<DurableRecord> reader,
        List<DurableRecord> batch
    )
    {
        batch.Clear();

        while (reader.TryRead(out DurableRecord? record))
        {
            batch.Add(record);
        }

        if (batch.Count == 0)
        {
            return;
        }

        bool persisted = await TryPersistBatchWithRetryAsync(batch, CancellationToken.None)
            .ConfigureAwait(false);
        if (persisted)
        {
            return;
        }

        await WriteDeadLetterAsync(batch).ConfigureAwait(false);
    }

    private async Task<bool> TryPersistBatchWithRetryAsync(
        List<DurableRecord> batch,
        CancellationToken ct
    )
    {
        for (int attempt = 0; attempt <= _retryAttempts; attempt++)
        {
            try
            {
                TurboDbContext db = await _dbContextFactory
                    .CreateDbContextAsync(ct)
                    .ConfigureAwait(false);

                try
                {
                    foreach (DurableRecord record in batch)
                    {
                        db.Add(MapEntity(record));
                    }

                    await db.SaveChangesAsync(ct).ConfigureAwait(false);
                }
                finally
                {
                    await db.DisposeAsync().ConfigureAwait(false);
                }

                return true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < _retryAttempts)
            {
                _logger.LogWarning(
                    TurboEventIds.AuditWriteRetry,
                    ex,
                    "Failed to persist a batch of {Count} durable observability record(s) (attempt {Attempt}/{Max}). Retrying.",
                    batch.Count,
                    attempt + 1,
                    _retryAttempts
                );

                if (_retryDelayMs > 0)
                {
                    await Task.Delay(_retryDelayMs, ct).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    TurboEventIds.AuditWriteFailed,
                    ex,
                    "Failed to persist a batch of {Count} durable observability record(s); moving to dead-letter.",
                    batch.Count
                );

                return false;
            }
        }

        return false;
    }

    private async Task WriteDeadLetterAsync(List<DurableRecord> batch)
    {
        if (string.IsNullOrWhiteSpace(_deadLetterPath))
        {
            return;
        }

        try
        {
            EnsureDirectory(_deadLetterPath);

            string payload = JsonSerializer.Serialize(
                new
                {
                    happenedAtUtc = DateTime.UtcNow,
                    records = batch.ConvertAll(MapDeadLetterPayload)
                },
                JsonOptions
            );

            await File.AppendAllTextAsync(
                    _deadLetterPath,
                    $"{payload}{Environment.NewLine}",
                    Encoding.UTF8
                )
                .ConfigureAwait(false);

            _logger.LogInformation(
                TurboEventIds.AuditWriteDeadLettered,
                "Wrote {Count} durable audit records to dead-letter file {Path}",
                batch.Count,
                _deadLetterPath
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                TurboEventIds.AuditDeadLetterWriteFailed,
                ex,
                "Failed to write {Count} durable observability records to dead-letter file {Path}.",
                batch.Count,
                _deadLetterPath
            );
        }
    }

    private static object MapDeadLetterPayload(DurableRecord record)
    {
        return record switch
        {
            AuditRecord audit => new
            {
                kind = nameof(AuditRecord),
                occurredAt = audit.OccurredAt,
                correlationId = audit.CorrelationId,
                audit = audit.Event
            },
            LedgerRecord ledger => new
            {
                kind = nameof(LedgerRecord),
                occurredAt = ledger.OccurredAt,
                correlationId = ledger.CorrelationId,
                ledger = ledger.Event
            },
            ItemRecord item => new
            {
                kind = nameof(ItemRecord),
                occurredAt = item.OccurredAt,
                correlationId = item.CorrelationId,
                item = item.Event
            },
            PerformanceLogRecord performanceLog => new
            {
                kind = nameof(PerformanceLogRecord),
                occurredAt = performanceLog.OccurredAt,
                correlationId = performanceLog.CorrelationId,
                performanceLog = performanceLog.Event
            },
            _ => throw new ArgumentOutOfRangeException(nameof(record), record.GetType().Name, null)
        };
    }

    private void EnsureDirectory(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    private static object MapEntity(DurableRecord record)
    {
        return record switch
        {
            AuditRecord audit => MapAudit(audit),
            LedgerRecord ledger => MapLedger(ledger),
            ItemRecord item => MapItem(item),
            PerformanceLogRecord performanceLog => MapPerformanceLog(performanceLog),
            _ => throw new ArgumentOutOfRangeException(
                nameof(record),
                record.GetType().Name,
                "Unknown durable observability record type."
            )
        };
    }

    private static AuditEventEntity MapAudit(AuditRecord record)
    {
        return new AuditEventEntity
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
            IpHash = record.Event.IpHash,
            Data = record.Event.Data
        };
    }

    private static EconomyLedgerEntity MapLedger(LedgerRecord record)
    {
        return new EconomyLedgerEntity
        {
            OccurredAt = record.OccurredAt,
            PlayerId = record.Event.PlayerId,
            Currency = record.Event.Currency,
            ActivityPointType = record.Event.ActivityPointType,
            Delta = record.Event.Delta,
            BalanceAfter = record.Event.BalanceAfter,
            Reason = record.Event.Reason,
            CorrelationId = record.CorrelationId,
            RefId = record.Event.RefId
        };
    }

    private static ItemEventEntity MapItem(ItemRecord record)
    {
        return new ItemEventEntity
        {
            OccurredAt = record.OccurredAt,
            ItemId = record.Event.ItemId,
            EventType = record.Event.EventType,
            ActorPlayerId = record.Event.ActorPlayerId,
            FromOwnerId = record.Event.FromOwnerId,
            ToOwnerId = record.Event.ToOwnerId,
            RoomId = record.Event.RoomId,
            CorrelationId = record.CorrelationId,
            Data = record.Event.Data
        };
    }

    private static PerformanceLogEntity MapPerformanceLog(PerformanceLogRecord record)
    {
        return new PerformanceLogEntity
        {
            ElapsedTime = record.Event.ElapsedTime,
            UserAgent = record.Event.UserAgent,
            FlashVersion = record.Event.FlashVersion,
            OS = record.Event.OS,
            Browser = record.Event.Browser,
            IsDebugger = record.Event.IsDebugger,
            MemoryUsage = record.Event.MemoryUsage,
            GarbageCollections = record.Event.GarbageCollections,
            AverageFrameRate = record.Event.AverageFrameRate,
            IPAddress = record.Event.IPAddress
        };
    }
}
