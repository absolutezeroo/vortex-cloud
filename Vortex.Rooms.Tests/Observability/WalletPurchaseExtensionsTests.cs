using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Vortex.Primitives.Commerce;
using Vortex.Primitives.Players.Enums.Wallet;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Players.Wallet;
using Xunit;

namespace Vortex.Rooms.Tests.Observability;

public sealed class WalletPurchaseExtensionsTests
{
    [Fact]
    public async Task InsufficientBalance_DoesNotGrantOrCreditBack()
    {
        RecordingWalletGrain wallet = new RecordingWalletGrain { DebitSucceeds = false };
        List<WalletDebitRequest> requests = Requests(10);
        bool grantInvoked = false;

        WalletPurchaseResult<int> result = await wallet.ExecutePurchaseAsync(
            requests,
            ct =>
            {
                grantInvoked = true;

                return Task.FromResult(1);
            },
            NullLogger.Instance,
            CancellationToken.None
        );

        result.Succeeded.Should().BeFalse();
        grantInvoked.Should().BeFalse();
        wallet.CreditBackCalls.Should().Be(0);
    }

    [Fact]
    public async Task SuccessfulGrant_DoesNotCreditBack()
    {
        RecordingWalletGrain wallet = new RecordingWalletGrain { DebitSucceeds = true };
        List<WalletDebitRequest> requests = Requests(10);

        WalletPurchaseResult<int> result = await wallet.ExecutePurchaseAsync(
            requests,
            ct => Task.FromResult(42),
            NullLogger.Instance,
            CancellationToken.None
        );

        result.Succeeded.Should().BeTrue();
        result.Reward.Should().Be(42);
        wallet.CreditBackCalls.Should().Be(0);
    }

    [Fact]
    public async Task GrantThrows_RefundsDebitedAmountAndRethrows()
    {
        RecordingWalletGrain wallet = new RecordingWalletGrain { DebitSucceeds = true };
        List<WalletDebitRequest> requests = Requests(10);
        InvalidOperationException thrown = new InvalidOperationException("grant failed");

        Func<Task> act = () =>
            wallet.ExecutePurchaseAsync<int>(
                requests,
                ct => throw thrown,
                NullLogger.Instance,
                CancellationToken.None
            );

        await act.Should().ThrowAsync<InvalidOperationException>();
        wallet.CreditBackCalls.Should().Be(1);
        wallet.CreditBackRequests.Should().BeSameAs(requests);
    }

    [Fact]
    public async Task GrantThrows_WithNoDebitRequests_DoesNotAttemptRefund()
    {
        RecordingWalletGrain wallet = new RecordingWalletGrain { DebitSucceeds = true };
        List<WalletDebitRequest> requests = new List<WalletDebitRequest>();

        Func<Task> act = () =>
            wallet.ExecutePurchaseAsync<int>(
                requests,
                ct => throw new InvalidOperationException("grant failed"),
                NullLogger.Instance,
                CancellationToken.None
            );

        await act.Should().ThrowAsync<InvalidOperationException>();
        wallet.CreditBackCalls.Should().Be(0);
    }

    /// <summary>
    /// Cancellation is the most common reason the grant step throws (client disconnect, host
    /// shutdown, timeout), so it is the case where the refund matters most -- and the one where
    /// refunding under the caller's own token would silently skip the refund and leave the player
    /// paid-for-nothing. Replacing <c>CancellationToken.None</c> with <c>ct</c> in
    /// <c>ExecutePurchaseAsync</c> is the edit this test exists to fail on.
    /// </summary>
    [Fact]
    public async Task GrantCancelled_StillRefundsOnAnUncancelledToken()
    {
        RecordingWalletGrain wallet = new RecordingWalletGrain { DebitSucceeds = true };
        List<WalletDebitRequest> requests = Requests(10);
        using CancellationTokenSource cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Func<Task> act = () =>
            wallet.ExecutePurchaseAsync<int>(
                requests,
                ct => Task.FromCanceled<int>(ct),
                NullLogger.Instance,
                cts.Token
            );

        await act.Should().ThrowAsync<OperationCanceledException>();
        wallet.CreditBackCalls.Should().Be(1);
        wallet.CreditBackSawCancelledToken.Should().BeFalse();
    }

    /// <summary>
    /// A refund that fails is already the worst case; swallowing the reason the purchase failed on
    /// top of it would leave nothing to diagnose from. The original exception is what propagates.
    /// </summary>
    [Fact]
    public async Task RefundFailing_DoesNotMaskTheOriginalFailure()
    {
        RecordingWalletGrain wallet = new RecordingWalletGrain
        {
            DebitSucceeds = true,
            CreditBackThrows = true,
        };

        Func<Task> act = () =>
            wallet.ExecutePurchaseAsync<int>(
                Requests(10),
                ct => throw new InvalidOperationException("grant failed"),
                NullLogger.Instance,
                CancellationToken.None
            );

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("grant failed");
        wallet.CreditBackCalls.Should().Be(1);
    }

    private static List<WalletDebitRequest> Requests(int amount)
    {
        return
        [
            new WalletDebitRequest
            {
                CurrencyKind = new CurrencyKind { CurrencyType = CurrencyType.Credits },
                Amount = amount,
            },
        ];
    }

    private sealed class RecordingWalletGrain : IPlayerWalletGrain
    {
        public bool DebitSucceeds { get; init; }

        public bool CreditBackThrows { get; init; }

        public int CreditBackCalls { get; private set; }

        public List<WalletDebitRequest>? CreditBackRequests { get; private set; }

        /// <summary>A real wallet grain call under a cancelled token would not run; recording the
        /// token's state at call time is how the test sees which token the refund was issued
        /// on.</summary>
        public bool CreditBackSawCancelledToken { get; private set; }

        public Task<WalletDebitResult> TryDebitAsync(
            List<WalletDebitRequest> requests,
            CancellationToken ct
        )
        {
            return Task.FromResult(
                DebitSucceeds
                    ? WalletDebitResult.Success()
                    : WalletDebitResult.InsufficientBalance(
                        new WalletDebitFailure
                        {
                            CurrencyKind = requests[0].CurrencyKind,
                            Amount = requests[0].Amount,
                        }
                    )
            );
        }

        public Task<WalletDebitResult> TryDebitAsync(
            List<WalletDebitRequest> requests,
            CommerceOperationId operationId,
            CancellationToken ct
        ) => TryDebitAsync(requests, ct);

        public Task CreditBackAsync(
            List<WalletDebitRequest> requests,
            CommerceOperationId operationId,
            CancellationToken ct
        ) => CreditBackAsync(requests, ct);

        public Task CreditBackAsync(List<WalletDebitRequest> requests, CancellationToken ct)
        {
            CreditBackCalls++;
            CreditBackRequests = requests;
            CreditBackSawCancelledToken = ct.IsCancellationRequested;

            return CreditBackThrows
                ? Task.FromException(new InvalidOperationException("wallet unreachable"))
                : Task.CompletedTask;
        }

        public Task<int> GetAmountForCurrencyAsync(CurrencyKind kind, CancellationToken ct)
        {
            return Task.FromResult(0);
        }

        public Task<Dictionary<int, int>> GetActivityPointsAsync(CancellationToken ct)
        {
            return Task.FromResult(new Dictionary<int, int>());
        }

        public Task GrantCreditsAsync(int amount, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task<bool> GrantCurrencyAsync(CurrencyKind kind, int amount, CancellationToken ct)
        {
            return Task.FromResult(true);
        }

        public Task GrantActivityPointsAsync(
            int activityPointType,
            int amount,
            CancellationToken ct
        )
        {
            return Task.CompletedTask;
        }
    }
}
