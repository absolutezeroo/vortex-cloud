using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
    public static async Task<WalletPurchaseResult<TReward>> ExecutePurchaseAsync<TReward>(
        this IPlayerWalletGrain wallet,
        List<WalletDebitRequest> debitRequests,
        Func<CancellationToken, Task<TReward>> grantAsync,
        ILogger logger,
        CancellationToken ct
    )
    {
        WalletDebitResult debitResult =
            debitRequests.Count > 0
                ? await wallet.TryDebitAsync(debitRequests, ct).ConfigureAwait(false)
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
                    await wallet.CreditBackAsync(debitRequests, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception refundEx)
                {
                    logger.LogCritical(
                        refundEx,
                        "REFUND FAILED after a failed grant - player may be permanently debited."
                    );
                }
            }

            throw;
        }
    }
}
