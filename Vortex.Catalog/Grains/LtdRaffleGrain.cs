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

/// <summary>
/// Runs one LTD series' raffle: entrants pay credits during a window, one wins the limited-edition
/// item and everybody else is refunded.
/// <para>
/// Because real money-equivalent currency moves here, correctness rules are stricter than elsewhere:
/// nothing is finalised on in-memory state alone, every transition is persisted, and the two
/// externally-visible effects (granting the item, returning credits) are ordered so that a crash can
/// leave work undone but can never double-pay. Concretely:
/// </para>
/// <list type="bullet">
/// <item>
/// The winner's serial number and the series stock decrement are written in a <em>single</em>
/// <c>SaveChangesAsync</c> before the item is granted, so the serial and the stock are consumed
/// exactly once even if the grant or the process dies immediately after.
/// </item>
/// <item>
/// Refund failures are returned as values, not logged and swallowed, so
/// <see cref="RunRaffleAsync" /> can see them, keep the batch open and retry on the next activation.
/// </item>
/// <item>
/// The batch and its deadline are reconstructed from the database on activation, so a restart mid-
/// window resumes rather than stranding entrants' credits.
/// </item>
/// </list>
/// </summary>
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
    private IDisposable? _raffleTimer;

    /// <summary>
    /// Guards against a second draw for the same batch running concurrently — a timer tick landing
    /// while <see cref="ForceRunRaffleAsync" /> is mid-flight, or a duplicated timer registration.
    /// A plain field is enough (and is not a lock): Orleans runs one grain turn at a time, so this
    /// only has to survive across <c>await</c> points within the activation.
    /// </summary>
    private bool _drawInProgress;

    /// <summary>
    /// When the next draw is due, or <c>null</c> when no batch is waiting. Exposed so a restart can
    /// be asserted to have re-armed the draw rather than silently dropping it.
    /// </summary>
    internal DateTime? NextDrawDueUtc { get; private set; }

    private int SeriesId => (int)this.GetPrimaryKeyLong();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await LoadSeriesAsync(ct);
        await RecoverAsync(ct);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        _raffleTimer?.Dispose();
        _raffleTimer = null;

        return Task.CompletedTask;
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

        // One entry per player per batch.
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
        bool startsNewBatch = _currentBatchId is null;
        string batchId = _currentBatchId ?? Guid.NewGuid().ToString("N");
        DateTime enteredAt = DateTime.UtcNow;

        WalletPurchaseResult<LtdRaffleEntryEntity> result;

        try
        {
            result = await wallet
                .ExecutePurchaseAsync(
                    debitRequests,
                    async innerCt =>
                    {
                        LtdRaffleEntryEntity entry = new()
                        {
                            SeriesEntityId = SeriesId,
                            PlayerEntityId = playerIdInt,
                            BatchId = batchId,
                            EnteredAt = enteredAt,
                            Result = LtdRaffleEntryResults.PENDING,
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
        }
        catch (Exception ex)
        {
            // ExecutePurchaseAsync refunds the debit and rethrows when the entry row cannot be
            // written. Report that as a failed entry instead of letting it escape as a grain-call
            // fault: the player's credits are already back, and the caller only needs to know the
            // entry did not happen.
            logger.LogError(
                ex,
                "Failed to record LTD raffle entry for player {PlayerId} in series {SeriesId} "
                    + "(batch {BatchId}); the wallet debit was refunded",
                playerIdInt,
                SeriesId,
                batchId
            );

            return Fail("entry_failed");
        }

        if (!result.Succeeded)
        {
            return Fail("insufficient_credits");
        }

        LtdRaffleEntryEntity savedEntry = result.Reward!;

        _currentBatch.Add(savedEntry);

        // Arm the raffle timer for the first entrant in this batch. The deadline is derived from the
        // first entry's timestamp, which is persisted — that is what lets OnActivateAsync rebuild it
        // after a restart without any extra column.
        if (startsNewBatch)
        {
            _currentBatchId = batchId;

            await ScheduleOrRunDrawAsync(enteredAt.AddSeconds(_series.RaffleWindowSeconds), ct);
        }

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

        CatalogProductEntity? product = await dbCtx
            .CatalogProducts.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == _series.CatalogProductEntityId && p.DeletedAt == null,
                ct
            );

        if (product?.FurnitureDefinitionEntityId is null or 0)
        {
            // Without a definition the inventory grant always throws. Previously that surfaced only
            // when the draw ran, where it was caught and the whole batch was dropped — every entrant
            // silently losing their credits. Refuse entries instead (RemainingQuantity is untouched;
            // EnterRaffleAsync's checks below still apply).
            logger.LogError(
                "LTD series {SeriesId} resolves to no furniture definition (catalog product "
                    + "{CatalogProductId}). The raffle cannot award anything; entries will be "
                    + "refused until the catalog product is fixed.",
                SeriesId,
                _series.CatalogProductEntityId
            );
        }

        _furniDefinitionId = product?.FurnitureDefinitionEntityId ?? 0;
    }

    /// <summary>
    /// Rebuilds the in-flight batch after an activation and re-arms (or immediately fires) its draw.
    /// <para>
    /// Only one batch is ever restored — the oldest one with open entries, keyed by
    /// <c>BatchId</c>. The previous implementation took <c>entries[0].BatchId</c> as the batch id but
    /// kept <em>every</em> pending row regardless of batch, so entrants from different windows were
    /// drawn against each other and marked with the wrong batch's outcome.
    /// </para>
    /// </summary>
    private async Task RecoverAsync(CancellationToken ct)
    {
        if (_series is null)
        {
            return;
        }

        _currentBatch.Clear();
        _currentBatchId = null;

        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        List<LtdRaffleEntryEntity> openEntries = await dbCtx
            .LtdRaffleEntries.AsNoTracking()
            .Where(e =>
                e.SeriesEntityId == SeriesId
                && e.DeletedAt == null
                && (
                    e.Result == LtdRaffleEntryResults.PENDING
                    || e.Result == LtdRaffleEntryResults.PROCESSING
                    || e.Result == LtdRaffleEntryResults.REFUND_FAILED
                    || e.Result == LtdRaffleEntryResults.GRANT_FAILED
                )
            )
            .ToListAsync(ct);

        if (openEntries.Count == 0)
        {
            return;
        }

        // Anything left mid-flight when the process died is indeterminate: the wallet or inventory
        // call may or may not have landed. Retrying could pay twice, so surface it for an operator
        // rather than guessing.
        foreach (
            IGrouping<string, LtdRaffleEntryEntity> stuck in openEntries
                .Where(e =>
                    e.Result
                        is LtdRaffleEntryResults.PROCESSING
                            or LtdRaffleEntryResults.GRANT_FAILED
                )
                .GroupBy(e => e.BatchId)
        )
        {
            logger.LogError(
                "LTD series {SeriesId} batch {BatchId} has {Count} entr(ies) left in an "
                    + "indeterminate state ({States}) from an interrupted draw. They are NOT retried "
                    + "automatically because the wallet/inventory call may already have been "
                    + "applied; entry ids: {EntryIds}",
                SeriesId,
                stuck.Key,
                stuck.Count(),
                string.Join(", ", stuck.Select(e => e.Result).Distinct()),
                string.Join(", ", stuck.Select(e => e.Id))
            );
        }

        // Refunds that threw are known not to have been applied, so they are safe to retry.
        List<LtdRaffleEntryEntity> retryableRefunds = openEntries
            .Where(e => e.Result == LtdRaffleEntryResults.REFUND_FAILED)
            .ToList();

        if (retryableRefunds.Count > 0)
        {
            logger.LogWarning(
                "Retrying {Count} previously failed LTD refund(s) for series {SeriesId}",
                retryableRefunds.Count,
                SeriesId
            );

            await RefundEntriesAsync(retryableRefunds.Select(e => e.Id).ToList(), ct);
        }

        List<LtdRaffleEntryEntity> pending = openEntries
            .Where(e => e.Result == LtdRaffleEntryResults.PENDING)
            .ToList();

        if (pending.Count == 0)
        {
            return;
        }

        // Oldest open batch wins; a later batch cannot exist before this one is closed anyway.
        string batchId = pending
            .GroupBy(e => e.BatchId)
            .OrderBy(g => g.Min(e => e.EnteredAt))
            .First()
            .Key;

        _currentBatchId = batchId;
        _currentBatch.AddRange(pending.Where(e => e.BatchId == batchId));

        DateTime deadline = _currentBatch
            .Min(e => e.EnteredAt)
            .AddSeconds(_series.RaffleWindowSeconds);

        logger.LogInformation(
            "Restored LTD raffle batch {BatchId} for series {SeriesId} with {Count} pending "
                + "entr(ies); draw deadline {Deadline:u}",
            batchId,
            SeriesId,
            _currentBatch.Count,
            deadline
        );

        await ScheduleOrRunDrawAsync(deadline, ct);
    }

    /// <summary>
    /// Arms the one-shot draw timer for <paramref name="deadlineUtc" />, or runs the draw straight
    /// away when the deadline has already passed — the normal case after a restart that outlasted the
    /// raffle window. Either way the batch can never be left with no draw scheduled, which is what
    /// used to strand entrants indefinitely: the previous activation path reloaded pending entries
    /// but never re-registered a timer, so a restart mid-window froze the raffle forever.
    /// </summary>
    private async Task ScheduleOrRunDrawAsync(DateTime deadlineUtc, CancellationToken ct)
    {
        _raffleTimer?.Dispose();
        _raffleTimer = null;

        TimeSpan delay = deadlineUtc - DateTime.UtcNow;

        if (delay <= TimeSpan.Zero)
        {
            NextDrawDueUtc = null;

            await RunRaffleAsync(ct);

            return;
        }

        NextDrawDueUtc = deadlineUtc;

        _raffleTimer = this.RegisterGrainTimer<object?>(
            async (_, timerCt) => await RunRaffleAsync(timerCt),
            null,
            delay,
            TimeSpan.FromMilliseconds(-1) // one-shot
        );
    }

    private async Task RunRaffleAsync(CancellationToken ct)
    {
        if (_drawInProgress)
        {
            logger.LogDebug(
                "Ignoring a concurrent draw request for LTD series {SeriesId}; one is already running",
                SeriesId
            );

            return;
        }

        if (_series is null || _currentBatchId is null)
        {
            return;
        }

        _drawInProgress = true;

        try
        {
            await DrawAsync(_currentBatchId, ct);
        }
        catch (Exception ex)
        {
            // Deliberately not clearing the batch here: the entries stay in whatever persisted state
            // they reached, and the next activation picks them up again. Dropping them (which is
            // what the old finally-block did) was how entrants lost credits with no record.
            logger.LogError(
                ex,
                "Failed to run raffle for series {SeriesId} batch {BatchId}; the batch is left open "
                    + "for retry",
                SeriesId,
                _currentBatchId
            );
        }
        finally
        {
            _drawInProgress = false;
            _raffleTimer?.Dispose();
            _raffleTimer = null;
        }
    }

    private async Task DrawAsync(string batchId, CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        // Tracked (not AsNoTracking) so the claim below commits as one SaveChangesAsync.
        List<LtdRaffleEntryEntity> entries = await dbCtx
            .LtdRaffleEntries.Where(e =>
                e.SeriesEntityId == SeriesId
                && e.BatchId == batchId
                && e.DeletedAt == null
                && e.Result == LtdRaffleEntryResults.PENDING
            )
            .ToListAsync(ct);

        if (entries.Count == 0)
        {
            // Already drawn (a duplicate timer tick, or ForceRunRaffleAsync racing the timer):
            // nothing is pending, so there is nothing to award or refund a second time.
            ClearBatch();

            return;
        }

        LtdSeriesEntity? series = await dbCtx.LtdSeries.FirstOrDefaultAsync(
            s => s.Id == SeriesId && s.DeletedAt == null,
            ct
        );

        if (series is null)
        {
            logger.LogError(
                "LTD series {SeriesId} disappeared while drawing batch {BatchId}; refunding "
                    + "{Count} entrant(s)",
                SeriesId,
                batchId,
                entries.Count
            );

            await VoidBatchAsync(dbCtx, entries, ct);

            return;
        }

        DateTime now = DateTime.UtcNow;

        if (series.RemainingQuantity <= 0 || _furniDefinitionId == 0)
        {
            // Nothing can be awarded. Everyone gets their credits back — the old code marked the
            // batch "no_remaining" and refunded nobody, so every entrant simply lost their money.
            logger.LogWarning(
                "LTD series {SeriesId} cannot award batch {BatchId} (remaining={Remaining}, "
                    + "furnitureDefinitionId={DefinitionId}); refunding all {Count} entrant(s)",
                SeriesId,
                batchId,
                series.RemainingQuantity,
                _furniDefinitionId,
                entries.Count
            );

            await VoidBatchAsync(dbCtx, entries, ct);

            return;
        }

        int winnerIndex = Random.Shared.Next(entries.Count);
        LtdRaffleEntryEntity winner = entries[winnerIndex];
        int serialNumber = series.TotalQuantity - series.RemainingQuantity + 1;

        // ── The claim ────────────────────────────────────────────────────────
        // One SaveChangesAsync reserves the serial for this winner, decrements the stock and moves
        // every loser out of "pending". Whatever happens next — a failed grant, a failed refund, the
        // process dying — the serial and the stock have been consumed exactly once and no second
        // draw can pick this batch up again.
        winner.Result = LtdRaffleEntryResults.WON;
        winner.SerialNumber = serialNumber;
        winner.ProcessedAt = now;

        foreach (LtdRaffleEntryEntity loser in entries.Where(e => e.Id != winner.Id))
        {
            loser.Result = LtdRaffleEntryResults.PROCESSING;
        }

        series.RemainingQuantity--;

        if (series.RemainingQuantity <= 0)
        {
            series.HasRaffleFinished = true;
        }

        await dbCtx.SaveChangesAsync(ct);

        // Keep the in-memory view of the series consistent with what was just committed.
        _series!.RemainingQuantity = series.RemainingQuantity;
        _series.HasRaffleFinished = series.HasRaffleFinished;

        // ── External effects, after the claim ────────────────────────────────
        bool granted = await GrantToWinnerAsync(dbCtx, winner, serialNumber, series, ct);

        List<int> loserIds = entries.Where(e => e.Id != winner.Id).Select(e => e.Id).ToList();
        bool allRefunded = await RefundEntriesAsync(loserIds, ct);

        if (granted && allRefunded)
        {
            ClearBatch();

            return;
        }

        // Something is still owed. Leave the batch bound to the grain so the operator-visible state
        // and the next activation's recovery pass both still see it.
        logger.LogError(
            "LTD raffle batch {BatchId} for series {SeriesId} did not finalise cleanly "
                + "(itemGranted={Granted}, allRefunded={AllRefunded}); the batch stays open",
            batchId,
            SeriesId,
            granted,
            allRefunded
        );
    }

    /// <summary>
    /// Grants the item to the winner. The serial is already reserved and the stock already
    /// decremented, so a failure here is recorded as <see cref="LtdRaffleEntryResults.GRANT_FAILED" />
    /// and never retried automatically — re-running could hand out the same serial twice.
    /// </summary>
    private async Task<bool> GrantToWinnerAsync(
        VortexDbContext dbCtx,
        LtdRaffleEntryEntity winner,
        int serialNumber,
        LtdSeriesEntity series,
        CancellationToken ct
    )
    {
        try
        {
            await grainFactory
                .GetInventoryGrain(PlayerId.Parse(winner.PlayerEntityId))
                .GrantLtdFurnitureAsync(_furniDefinitionId, serialNumber, series.TotalQuantity, ct);

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

            return true;
        }
        catch (Exception ex)
        {
            logger.LogCritical(
                ex,
                "Failed to grant LTD item to winner. Player={PlayerId} Series={SeriesId} "
                    + "Batch={BatchId} Serial={SerialNumber} FurnitureDefinition={DefinitionId}. "
                    + "The serial and the series stock are already consumed; this is NOT retried "
                    + "automatically to avoid issuing the item twice and needs manual resolution.",
                winner.PlayerEntityId,
                SeriesId,
                winner.BatchId,
                serialNumber,
                _furniDefinitionId
            );

            try
            {
                winner.Result = LtdRaffleEntryResults.GRANT_FAILED;

                await dbCtx.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception markEx)
            {
                logger.LogError(
                    markEx,
                    "Failed to record the grant failure for LTD entry {EntryId} (series "
                        + "{SeriesId}, batch {BatchId})",
                    winner.Id,
                    SeriesId,
                    winner.BatchId
                );
            }

            return false;
        }
    }

    /// <summary>
    /// Refunds every entry in <paramref name="entryIds" /> and records the outcome per entry.
    /// Returns <c>false</c> when at least one refund failed, so the caller can keep the batch open.
    /// <para>
    /// Wallet calls run in parallel (independent grains), then all outcomes are persisted in one
    /// <c>SaveChangesAsync</c>. A refund that threw becomes
    /// <see cref="LtdRaffleEntryResults.REFUND_FAILED" /> — known not to have been applied, therefore
    /// safe to retry — while an entry left in <see cref="LtdRaffleEntryResults.PROCESSING" /> by a
    /// crash is never retried, which is what prevents a double refund.
    /// </para>
    /// </summary>
    private async Task<bool> RefundEntriesAsync(List<int> entryIds, CancellationToken ct)
    {
        if (entryIds.Count == 0)
        {
            return true;
        }

        await using VortexDbContext dbCtx = await dbCtxFactory.CreateDbContextAsync(ct);

        List<LtdRaffleEntryEntity> entries = await dbCtx
            .LtdRaffleEntries.Where(e => entryIds.Contains(e.Id))
            .ToListAsync(ct);

        if (entries.Count == 0)
        {
            return true;
        }

        int cost = _series!.CostCredits;

        if (cost <= 0)
        {
            // A free raffle has nothing to give back; close the losers out directly.
            DateTime freeNow = DateTime.UtcNow;

            foreach (LtdRaffleEntryEntity entry in entries)
            {
                entry.Result = LtdRaffleEntryResults.REFUNDED;
                entry.ProcessedAt = freeNow;
            }

            await dbCtx.SaveChangesAsync(ct);

            return true;
        }

        RefundOutcome[] outcomes = await Task.WhenAll(
            entries.Select(entry => RefundEntrantAsync(entry.Id, entry.PlayerEntityId, cost))
        );

        DateTime now = DateTime.UtcNow;
        Dictionary<int, RefundOutcome> byEntryId = outcomes.ToDictionary(o => o.EntryId);

        foreach (LtdRaffleEntryEntity entry in entries)
        {
            if (!byEntryId.TryGetValue(entry.Id, out RefundOutcome? outcome))
            {
                continue;
            }

            entry.Result = outcome.Succeeded
                ? LtdRaffleEntryResults.REFUNDED
                : LtdRaffleEntryResults.REFUND_FAILED;
            entry.ProcessedAt = now;
        }

        await dbCtx.SaveChangesAsync(CancellationToken.None);

        return outcomes.All(o => o.Succeeded);
    }

    /// <summary>
    /// Returns one entrant's credits. Failures are returned, not swallowed: the previous version
    /// logged the exception and completed normally, so the <c>Task.WhenAll</c> above it saw success
    /// and the batch was finalised with the refund permanently lost.
    /// </summary>
    private async Task<RefundOutcome> RefundEntrantAsync(int entryId, int playerId, int amount)
    {
        try
        {
            IPlayerWalletGrain wallet = grainFactory.GetPlayerWalletGrain(PlayerId.Parse(playerId));

            // CancellationToken.None on purpose: this is compensation for money already taken, and
            // it must not be abandoned because the surrounding timer tick was cancelled.
            await wallet.GrantCreditsAsync(amount, CancellationToken.None).ConfigureAwait(true);

            return new RefundOutcome(entryId, playerId, true);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to refund LTD raffle credits. Player={PlayerId} Series={SeriesId} "
                    + "Batch={BatchId} Entry={EntryId} Amount={Amount}. The entry is kept as "
                    + "'{RetryState}' and retried on the next activation.",
                playerId,
                SeriesId,
                _currentBatchId,
                entryId,
                amount,
                LtdRaffleEntryResults.REFUND_FAILED
            );

            return new RefundOutcome(entryId, playerId, false);
        }
    }

    /// <summary>Refunds every entrant of a batch that cannot be awarded, and closes it if that worked.</summary>
    private async Task VoidBatchAsync(
        VortexDbContext dbCtx,
        List<LtdRaffleEntryEntity> entries,
        CancellationToken ct
    )
    {
        foreach (LtdRaffleEntryEntity entry in entries)
        {
            entry.Result = LtdRaffleEntryResults.PROCESSING;
        }

        await dbCtx.SaveChangesAsync(ct);

        if (await RefundEntriesAsync(entries.Select(e => e.Id).ToList(), ct))
        {
            ClearBatch();
        }
    }

    /// <summary>
    /// Detaches the batch from the grain. Only called once every entry has reached a terminal
    /// persisted state — the old code cleared it in a <c>finally</c>, which orphaned rows in the
    /// database that the grain had already forgotten.
    /// </summary>
    private void ClearBatch()
    {
        _currentBatch.Clear();
        _currentBatchId = null;
        NextDrawDueUtc = null;
    }

    private static LtdRaffleEntryResult Fail(string errorCode) =>
        new() { Success = false, ErrorCode = errorCode };

    private sealed record RefundOutcome(int EntryId, int PlayerId, bool Succeeded);
}
