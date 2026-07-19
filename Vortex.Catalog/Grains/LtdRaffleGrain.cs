using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Catalog;
using Vortex.Database.Entities.Players;
using Vortex.Primitives.Catalog.Grains;
using Vortex.Primitives.Catalog.Snapshots;
using Vortex.Primitives.Events;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Wallet;

namespace Vortex.Catalog.Grains;

public sealed class LtdRaffleGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IGrainFactory grainFactory,
    IEventPublisher events,
    ILogger<LtdRaffleGrain> logger
) : Grain, ILtdRaffleGrain
{
    private LtdSeriesEntity? _series;
    private int _furniDefinitionId;
    private readonly List<LtdRaffleEntryEntity> _currentBatch = [];
    private string? _currentBatchId;

    private int SeriesId => (int)this.GetPrimaryKeyLong();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await LoadSeriesAsync(ct);
    }

    // ── ILtdRaffleGrain ──────────────────────────────────────────────────────

    public async Task<LtdRaffleEntryResult> EnterRaffleAsync(
        PlayerId playerId,
        CancellationToken ct
    )
    {
        if (_series is null)
        {
            return Fail("series_not_found");
        }

        if (!_series.IsActive)
        {
            return Fail("series_not_active");
        }

        if (_series.HasRaffleFinished)
        {
            return Fail("raffle_finished");
        }

        if (_series.RemainingQuantity <= 0)
        {
            return Fail("no_remaining");
        }

        int playerIdInt = playerId.Value;

        // One entry per player per series
        if (_currentBatch.Any(e => e.PlayerEntityId == playerIdInt))
        {
            return Fail("already_entered");
        }

        // Resolve the entities the entry needs *before* touching the wallet, so a missing
        // player/series never leaves credits debited with nothing recorded to show for it.
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        PlayerEntity? playerEntity = await dbCtx.Players.FindAsync([playerIdInt], ct);
        if (playerEntity is null)
        {
            return Fail("player_not_found");
        }

        LtdSeriesEntity? seriesEntity = await dbCtx.LtdSeries.FindAsync([SeriesId], ct);
        if (seriesEntity is null)
        {
            return Fail("series_not_found");
        }

        List<WalletDebitRequest> debitRequests =
            _series.CostCredits > 0
                ?
                [
                    new WalletDebitRequest
                    {
                        CurrencyKind = new CurrencyKind { CurrencyType = CurrencyType.Credits },
                        Amount = _series.CostCredits,
                    },
                ]
                : [];

        IPlayerWalletGrain wallet = grainFactory.GetPlayerWalletGrain(playerId);
        string batchId = _currentBatchId ?? Guid.NewGuid().ToString("N");

        WalletPurchaseResult<LtdRaffleEntryEntity> result = await wallet
            .ExecutePurchaseAsync(
                debitRequests,
                async innerCt =>
                {
                    LtdRaffleEntryEntity entry = new()
                    {
                        SeriesEntityId = SeriesId,
                        PlayerEntityId = playerIdInt,
                        BatchId = batchId,
                        EnteredAt = DateTime.UtcNow,
                        Result = "pending",
                        SeriesEntity = seriesEntity,
                        PlayerEntity = playerEntity,
                    };

                    dbCtx.LtdRaffleEntries.Add(entry);
                    await dbCtx.SaveChangesAsync(innerCt);

                    return entry;
                },
                logger,
                ct
            )
            .ConfigureAwait(true);

        if (!result.Succeeded)
        {
            return Fail("insufficient_credits");
        }

        LtdRaffleEntryEntity savedEntry = result.Reward!;

        // Arm the raffle timer for the first entrant in this batch.
        if (_currentBatchId is null)
        {
            _currentBatchId = batchId;

            this.RegisterGrainTimer<object?>(
                async (_, ct) => await RunRaffleAsync(ct),
                null,
                TimeSpan.FromSeconds(_series.RaffleWindowSeconds),
                TimeSpan.FromMilliseconds(-1) // one-shot
            );
        }

        _currentBatch.Add(savedEntry);

        await events
            .PublishAsync(new LtdRaffleEnteredEvent(playerIdInt, SeriesId, _series.CostCredits), ct)
            .ConfigureAwait(true);

        return new LtdRaffleEntryResult { Success = true, ErrorCode = string.Empty };
    }

    public Task<UpcomingLtdSnapshot?> GetUpcomingLtdAsync(CancellationToken ct)
    {
        if (_series is null || !_series.IsActive || _series.HasRaffleFinished)
        {
            return Task.FromResult<UpcomingLtdSnapshot?>(null);
        }

        return Task.FromResult<UpcomingLtdSnapshot?>(
            new UpcomingLtdSnapshot
            {
                SeriesId = SeriesId,
                FurniDefinitionId = _furniDefinitionId,
                TotalQuantity = _series.TotalQuantity,
                RemainingQuantity = _series.RemainingQuantity,
                Price = _series.CostCredits,
                CurrencyType = 1, // credits
            }
        );
    }

    public Task<LtdSeriesSnapshot?> GetSeriesSnapshotAsync(CancellationToken ct)
    {
        if (_series is null)
        {
            return Task.FromResult<LtdSeriesSnapshot?>(null);
        }

        return Task.FromResult<LtdSeriesSnapshot?>(
            new LtdSeriesSnapshot
            {
                SeriesId = SeriesId,
                CatalogProductId = _series.CatalogProductEntityId,
                TotalQuantity = _series.TotalQuantity,
                RemainingQuantity = _series.RemainingQuantity,
                CostCredits = _series.CostCredits,
                RaffleWindowSeconds = _series.RaffleWindowSeconds,
                IsActive = _series.IsActive,
                HasRaffleFinished = _series.HasRaffleFinished,
            }
        );
    }

    public async Task ForceRunRaffleAsync(CancellationToken ct)
    {
        await RunRaffleAsync(ct);
    }

    public async Task ReloadSeriesAsync(CancellationToken ct)
    {
        await LoadSeriesAsync(ct);
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    private async Task LoadSeriesAsync(CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        _series = await dbCtx
            .LtdSeries.Include(s => s.CatalogProductEntity)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == SeriesId && s.DeletedAt == null, ct);

        if (_series is null)
        {
            logger.LogWarning("LtdRaffleGrain activated for unknown series {SeriesId}", SeriesId);
            return;
        }

        // Resolve furniture definition id from catalog product
        await using VortexDbContext dbCtx2 = await dbCtxFactory.CreateDbContextAsync(ct);

        CatalogProductEntity? product = await dbCtx2
            .CatalogProducts.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == _series.CatalogProductEntityId && p.DeletedAt == null,
                ct
            );

        _furniDefinitionId = product?.FurnitureDefinitionEntityId ?? 0;

        // Reload current in-flight batch entries
        _currentBatch.Clear();
        _currentBatchId = null;

        List<LtdRaffleEntryEntity> entries = await dbCtx2
            .LtdRaffleEntries.AsNoTracking()
            .Where(e =>
                e.SeriesEntityId == SeriesId && e.Result == "pending" && e.DeletedAt == null
            )
            .ToListAsync(ct);

        if (entries.Count > 0)
        {
            _currentBatchId = entries[0].BatchId;
            _currentBatch.AddRange(entries);
        }
    }

    private async Task RunRaffleAsync(CancellationToken ct)
    {
        if (_series is null || _currentBatch.Count == 0)
        {
            _currentBatchId = null;
            _currentBatch.Clear();
            return;
        }

        if (_series.RemainingQuantity <= 0)
        {
            await MarkBatchResultsAsync("no_remaining", null, ct);
            return;
        }

        // Select winner randomly
        int winnerIndex = Random.Shared.Next(_currentBatch.Count);
        LtdRaffleEntryEntity winner = _currentBatch[winnerIndex];
        int serialNumber = _series.TotalQuantity - _series.RemainingQuantity + 1;

        try
        {
            // Grant furniture to winner
            await grainFactory
                .GetInventoryGrain(PlayerId.Parse(winner.PlayerEntityId))
                .GrantLtdFurnitureAsync(
                    _furniDefinitionId,
                    serialNumber,
                    _series.TotalQuantity,
                    ct
                );

            await events
                .PublishAsync(
                    new LtdRaffleWonEvent(
                        winner.PlayerEntityId,
                        SeriesId,
                        serialNumber,
                        _furniDefinitionId
                    ),
                    ct
                )
                .ConfigureAwait(true);

            // Refund non-winners — await all refunds before finalizing the batch so a
            // failed refund is never silently lost once the results are persisted.
            if (_series.CostCredits > 0)
            {
                List<Task> refundTasks = [];

                foreach (LtdRaffleEntryEntity entry in _currentBatch)
                {
                    if (entry.PlayerEntityId == winner.PlayerEntityId)
                    {
                        continue;
                    }

                    refundTasks.Add(RefundEntrantAsync(entry.PlayerEntityId, ct));
                }

                await Task.WhenAll(refundTasks);
            }

            // Persist results
            await MarkBatchResultsAsync("lost", winner, ct);

            // Decrement remaining
            _series.RemainingQuantity--;

            await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);
            await dbCtx
                .LtdSeries.Where(s => s.Id == SeriesId)
                .ExecuteUpdateAsync(
                    up => up.SetProperty(p => p.RemainingQuantity, _series.RemainingQuantity),
                    ct
                );

            if (_series.RemainingQuantity <= 0)
            {
                _series.HasRaffleFinished = true;
                await dbCtx
                    .LtdSeries.Where(s => s.Id == SeriesId)
                    .ExecuteUpdateAsync(up => up.SetProperty(p => p.HasRaffleFinished, true), ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to run raffle for series {SeriesId}", SeriesId);
        }
        finally
        {
            _currentBatch.Clear();
            _currentBatchId = null;
        }
    }

    private async Task MarkBatchResultsAsync(
        string loserResult,
        LtdRaffleEntryEntity? winner,
        CancellationToken ct
    )
    {
        if (_currentBatch.Count == 0)
        {
            return;
        }

        List<int> batchIds = _currentBatch.Select(e => e.Id).ToList();
        DateTime now = DateTime.UtcNow;

        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        await dbCtx
            .LtdRaffleEntries.Where(e =>
                batchIds.Contains(e.Id) && e.Id != (winner != null ? winner.Id : -1)
            )
            .ExecuteUpdateAsync(
                up =>
                    up.SetProperty(p => p.Result, loserResult).SetProperty(p => p.ProcessedAt, now),
                ct
            );

        if (winner is not null)
        {
            await dbCtx
                .LtdRaffleEntries.Where(e => e.Id == winner.Id)
                .ExecuteUpdateAsync(
                    up =>
                        up.SetProperty(p => p.Result, "won")
                            .SetProperty(p => p.ProcessedAt, now)
                            .SetProperty(
                                p => p.SerialNumber,
                                _series!.TotalQuantity - _series.RemainingQuantity + 1
                            ),
                    ct
                );
        }
    }

    private static LtdRaffleEntryResult Fail(string errorCode) =>
        new() { Success = false, ErrorCode = errorCode };

    private async Task RefundEntrantAsync(int playerId, CancellationToken ct)
    {
        try
        {
            IPlayerWalletGrain wallet = grainFactory.GetPlayerWalletGrain(PlayerId.Parse(playerId));

            await wallet.GrantCreditsAsync(_series!.CostCredits, ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to refund raffle credits for player {PlayerId} in series {SeriesId}",
                playerId,
                SeriesId
            );
        }
    }
}
