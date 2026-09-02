using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Vortex.Primitives.Commerce;

/// <summary>
/// The other half of <see cref="Players.Wallet.WalletPurchaseExtensions"/>: that one covers a flow
/// whose failure is compensated, this one covers a flow whose failure is <em>owed</em>.
/// </summary>
public static class CommerceJournalExtensions
{
    /// <summary>
    /// Runs a payout that is already owed to the player, under an operation that says so.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For every flow that commits the reason for a reward before handing the reward over — a quest
    /// row marked complete, an achievement level persisted, a furniture consumed to open it. That
    /// order is deliberate everywhere it appears (the reverse lets a repeated click mint prizes), and
    /// its cost is that from the commit onwards there is nothing left to compensate: the player is
    /// owed, and a payout that never lands has to be findable. It was not — the flag was saved, the
    /// grant threw, and nothing anywhere recorded that somebody was short (PROG-REWARD-032,
    /// RSYS-PRIZE-050, ECON-CHEST-015).
    /// </para>
    /// <para>
    /// So the operation is opened <b>already past its pivot</b>. The only two ends are
    /// <see cref="CommerceOperationState.Completed"/> and
    /// <see cref="CommerceOperationState.NeedsIntervention"/>; there is no refund from here, and
    /// inventing one after a pivot is how one bug becomes two.
    /// </para>
    /// <para>
    /// A journal that is down never costs the player the payout: the reason it is owed is already
    /// committed, so refusing to pay because the bookkeeping failed turns a lost note into a lost
    /// reward. The payout's own exception is rethrown — callers behave in their own way when a grant
    /// fails, and this exists to record what happened, not to change it.
    /// </para>
    /// </remarks>
    public static async Task RecordOwedPayoutAsync(
        this ICommerceJournal journal,
        CommerceOperationKind kind,
        int playerId,
        string detail,
        Func<CancellationToken, Task> payAsync,
        ILogger logger,
        CancellationToken ct
    )
    {
        CommerceOperationId operation = CommerceOperationId.New();

        try
        {
            await journal.OpenAsync(operation, kind, playerId, detail, ct).ConfigureAwait(false);

            await journal
                .TransitionAsync(
                    operation,
                    CommerceOperationState.Pivoted,
                    CommerceStepKeys.REWARD_PAYOUT,
                    null,
                    ct
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Could not open {Kind} operation {OperationId} for player {PlayerId}; paying out anyway.",
                kind,
                operation,
                playerId
            );

            await payAsync(ct).ConfigureAwait(false);

            return;
        }

        try
        {
            await payAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await CloseAsync(
                    journal,
                    operation,
                    CommerceOperationState.NeedsIntervention,
                    ex.Message,
                    logger
                )
                .ConfigureAwait(false);

            throw;
        }

        await CloseAsync(journal, operation, CommerceOperationState.Completed, null, logger)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Closes the operation without ever letting the journal's own failure become the caller's. On
    /// the success path that would turn a paid reward into a thrown one; on the failure path it
    /// would replace the reason the payout failed with a reason about bookkeeping.
    /// </summary>
    private static async Task CloseAsync(
        ICommerceJournal journal,
        CommerceOperationId operation,
        CommerceOperationState state,
        string? error,
        ILogger logger
    )
    {
        try
        {
            await journal
                .TransitionAsync(
                    operation,
                    state,
                    CommerceStepKeys.REWARD_PAYOUT,
                    error,
                    CancellationToken.None
                )
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Could not close operation {OperationId} as {State}.",
                operation,
                state
            );
        }
    }
}
