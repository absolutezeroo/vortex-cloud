using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Commerce;
using Vortex.Primitives.Players.Wallet;

namespace Vortex.Primitives.Players.Grains;

public interface IPlayerWalletGrain : IGrainWithIntegerKey
{
    public Task<WalletDebitResult> TryDebitAsync(
        List<WalletDebitRequest> requests,
        CancellationToken ct
    );

    /// <summary>
    /// Debits the wallet as part of a named operation, and records that it did so inside the same
    /// transaction as the debit itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A replay — the same operation asking again after a timeout, a retry, or a restart — loses the
    /// receipt insert, rolls the whole attempt back and gets the earlier answer instead. Without this
    /// there was no way to tell a retry from a second purchase: the contract carried no identity at
    /// all, so a debit that timed out after committing looked exactly like one that never happened.
    /// </para>
    /// <para>
    /// The transaction already existed. <see cref="TryDebitAsync(List{WalletDebitRequest}, CancellationToken)"/>
    /// opens one, with an execution strategy and a fresh context per attempt; the receipt joins it
    /// rather than adding a second durable write to keep consistent with the first.
    /// </para>
    /// </remarks>
    public Task<WalletDebitResult> TryDebitAsync(
        List<WalletDebitRequest> requests,
        CommerceOperationId operationId,
        CancellationToken ct
    );

    public Task CreditBackAsync(List<WalletDebitRequest> requests, CancellationToken ct);

    /// <summary>
    /// Credits back a debit that belongs to a named operation, once. The refund and its receipt are
    /// one commit, so a retried compensation can neither double-credit nor be lost.
    /// </summary>
    /// <remarks>
    /// Only correct <em>before</em> an operation's pivot. After the pivot the operation is owed to
    /// the player and the answer to a failure is to finish it, never to take the goods' price back.
    /// </remarks>
    public Task CreditBackAsync(
        List<WalletDebitRequest> requests,
        CommerceOperationId operationId,
        CancellationToken ct
    );

    /// <summary>
    /// Credits the given amounts once for a named step of a named operation. The credits and the
    /// receipt commit together, so the step can be retried freely and cannot pay twice.
    /// </summary>
    /// <remarks>
    /// This is the general form; <see cref="CreditBackAsync(List{WalletDebitRequest}, CommerceOperationId, CancellationToken)"/>
    /// is it under the refund step. Paying a marketplace seller what their sold offers owe them is
    /// the same mechanism and emphatically not a refund, so it gets its own step key.
    /// </remarks>
    public Task<bool> CreditOnceAsync(
        List<WalletDebitRequest> credits,
        CommerceOperationId operationId,
        string stepKey,
        CancellationToken ct
    );
    public Task<int> GetAmountForCurrencyAsync(CurrencyKind kind, CancellationToken ct);
    public Task<Dictionary<int, int>> GetActivityPointsAsync(CancellationToken ct);
    public Task GrantCreditsAsync(int amount, CancellationToken ct);
    public Task GrantActivityPointsAsync(int activityPointType, int amount, CancellationToken ct);

    /// <summary>
    /// Credits any currency, including the ones with no dedicated grant of their own. Returns
    /// whether the amount actually landed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Silver and emeralds had no way in at all before this: they could be read and debited — the
    /// catalogue prices offers in silver — but nothing anywhere could increase them, so both sat at
    /// zero for the lifetime of an account and a silver-priced offer was unbuyable by construction.
    /// The grain has always been able to do this; only the two typed wrappers were on the interface.
    /// </para>
    /// <para>
    /// The result is not decoration. A currency with no <c>currency_types</c> row, or one that is
    /// disabled, cannot be credited — and a grant that quietly does nothing is worse than one that
    /// fails, because every layer above it reports success. Callers that can surface a failure
    /// should.
    /// </para>
    /// </remarks>
    public Task<bool> GrantCurrencyAsync(CurrencyKind kind, int amount, CancellationToken ct);
}
