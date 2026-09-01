using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vortex.Primitives.Commerce;
using Vortex.Primitives.Players.Grains;

namespace Vortex.Primitives.Players.Wallet;

public readonly record struct WalletPurchaseResult<TReward>
{
    public required bool Succeeded { get; init; }
    public TReward? Reward { get; init; }
    public WalletDebitFailure? Failure { get; init; }

    public static WalletPurchaseResult<TReward> Success(TReward reward) =>
        new() { Succeeded = true, Reward = reward };

    public static WalletPurchaseResult<TReward> InsufficientBalance(WalletDebitFailure? failure) =>
        new() { Succeeded = false, Failure = failure };
}

/// <summary>
/// Shared debit-then-grant executor for wallet-funded purchases (catalog offers, marketplace
/// buys, raffle entries, ...). Debits the wallet once, then runs the caller-supplied grant step;
/// if the grant throws, the debited amount is credited back before the exception is rethrown, so
/// a failure to deliver the purchase can never leave the player's credits permanently deducted.
/// </summary>
public static class WalletPurchaseExtensions
{
    public static Task<WalletPurchaseResult<TReward>> ExecutePurchaseAsync<TReward>(
        this IPlayerWalletGrain wallet,
        List<WalletDebitRequest> debitRequests,
        Func<CancellationToken, Task<TReward>> grantAsync,
        ILogger logger,
        CancellationToken ct
    ) =>
        wallet.ExecutePurchaseAsync(
            debitRequests,
            CommerceOperationId.None,
            grantAsync,
            logger,
            ct
        );

    /// <summary>
    /// The same debit-then-grant, under a named operation: the debit and its receipt commit together,
    /// and the compensating credit is applied once however many times it is asked for.
    /// </summary>
    /// <remarks>
    /// This is still the pre-pivot half of a flow. Everything it does is compensable by construction;
    /// what a flow does <em>after</em> its pivot belongs to the journal, not here, because there is no
    /// compensation past a pivot — only completion or an operator.
    /// </remarks>
    /// <param name="journal">
    /// Where the compensation is recorded when the grant throws. Optional only because the
    /// no-operation overload above has nothing to record against; a flow that passes an operation id
    /// and omits this leaves its compensated failures sitting at <c>Debited</c> forever, which reads
    /// from the outside exactly like an operation that was never finished.
    /// </param>
    public static async Task<WalletPurchaseResult<TReward>> ExecutePurchaseAsync<TReward>(
        this IPlayerWalletGrain wallet,
        List<WalletDebitRequest> debitRequests,
        CommerceOperationId operationId,
        Func<CancellationToken, Task<TReward>> grantAsync,
        ILogger logger,
        CancellationToken ct,
        ICommerceJournal? journal = null
    )
    {
        WalletDebitResult debitResult =
            debitRequests.Count > 0
                ? await wallet.TryDebitAsync(debitRequests, operationId, ct).ConfigureAwait(false)
                : WalletDebitResult.Success();

        if (!debitResult.Succeeded)
        {
            return WalletPurchaseResult<TReward>.InsufficientBalance(debitResult.Failure);
        }

        try
        {
            TReward reward = await grantAsync(ct).ConfigureAwait(false);

            return WalletPurchaseResult<TReward>.Success(reward);
        }
        catch (Exception ex)
        {
            // Nothing was taken, so there is nothing to put back and nothing to say about it.
            bool compensated = debitRequests.Count == 0;

            if (debitRequests.Count > 0)
            {
                logger.LogError(
                    ex,
                    "Purchase grant failed after wallet debit; refunding {RequestCount} debited amount(s).",
                    debitRequests.Count
                );

                // The compensation must not be subject to the token of the operation it is
                // compensating for: cancellation is the most common reason grantAsync throws
                // (client disconnect, host shutdown, timeout), and using `ct` here would make
                // the refund fail in exactly that case, leaving the player permanently debited.
                try
                {
                    await wallet
                        .CreditBackAsync(debitRequests, operationId, CancellationToken.None)
                        .ConfigureAwait(false);

                    compensated = true;
                }
                catch (Exception refundEx)
                {
                    logger.LogCritical(
                        refundEx,
                        "REFUND FAILED after a failed grant - player may be permanently debited."
                    );
                }
            }

            // Say that it was put right, and only then. Without this the operation stays at Debited,
            // which is the same thing the journal shows for a purchase still running and for one
            // that died halfway -- so a compensated failure was indistinguishable from work somebody
            // still has to finish, in every flow using this, the catalogue included. A refund that
            // itself failed is deliberately left at Debited: the player really is still out of
            // pocket, and that is exactly the operation an operator should find.
            if (compensated && !operationId.IsNone && journal is not null)
            {
                try
                {
                    await journal
                        .TransitionAsync(
                            operationId,
                            CommerceOperationState.FailedBeforePivot,
                            CommerceStepKeys.REFUND,
                            ex.Message,
                            CancellationToken.None
                        )
                        .ConfigureAwait(false);
                }
                catch (Exception journalEx)
                {
                    // The money is already back; losing the note is not worth replacing the
                    // caller's exception with this one, which would hide why the grant failed.
                    logger.LogError(
                        journalEx,
                        "Could not record the compensation of operation {OperationId}.",
                        operationId
                    );
                }
            }

            throw;
        }
    }
}
