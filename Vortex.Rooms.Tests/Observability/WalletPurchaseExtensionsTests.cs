using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
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

        public int CreditBackCalls { get; private set; }

        public List<WalletDebitRequest>? CreditBackRequests { get; private set; }

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

        public Task CreditBackAsync(List<WalletDebitRequest> requests, CancellationToken ct)
        {
            CreditBackCalls++;
            CreditBackRequests = requests;

            return Task.CompletedTask;
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
