using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Turbo.Database.Context;
using Turbo.Database.Entities.Wired;

namespace Turbo.Rooms.Wired.Logs;

/// <summary>
/// Single background consumer of <see cref="RoomWiredLogChannel"/>. Batches wired-execution log
/// entries and persists them with one <c>SaveChanges</c> per batch, keeping DB writes off the
/// wired-execution hot path — mirrors <c>Turbo.Observability.Audit.AuditWriterService</c>.
/// </summary>
public sealed class RoomWiredLogWriterService(
    RoomWiredLogChannel channel,
    IDbContextFactory<TurboDbContext> dbContextFactory,
    ILogger<RoomWiredLogWriterService> logger
) : BackgroundService
{
    private const int BatchSize = 100;

    private readonly RoomWiredLogChannel _channel = channel;
    private readonly IDbContextFactory<TurboDbContext> _dbContextFactory = dbContextFactory;
    private readonly ILogger<RoomWiredLogWriterService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ChannelReader<RoomWiredLogEntry> reader = _channel.Reader;
        List<RoomWiredLogEntry> batch = new(BatchSize);

        try
        {
            while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                while (batch.Count < BatchSize && reader.TryRead(out RoomWiredLogEntry? entry))
                {
                    batch.Add(entry);
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

        batch.Clear();

        while (reader.TryRead(out RoomWiredLogEntry? entry))
        {
            batch.Add(entry);
        }

        if (batch.Count > 0)
        {
            await FlushAsync(batch, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task FlushAsync(List<RoomWiredLogEntry> batch, CancellationToken ct)
    {
        try
        {
            await using TurboDbContext dbCtx = await _dbContextFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            foreach (RoomWiredLogEntry entry in batch)
            {
                dbCtx.RoomWiredLogs.Add(
                    new RoomWiredLogEntity
                    {
                        RoomEntityId = entry.RoomId,
                        LogLevel = entry.LogLevel,
                        LogSource = entry.LogSource,
                        Message = entry.Message,
                    }
                );
            }

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to persist a batch of {Count} wired room-log entries; entries dropped.",
                batch.Count
            );
        }
    }
}
