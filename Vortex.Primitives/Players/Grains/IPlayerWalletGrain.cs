using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Players.Wallet;

namespace Vortex.Primitives.Players.Grains;

public interface IPlayerWalletGrain : IGrainWithIntegerKey
{
    public Task<WalletDebitResult> TryDebitAsync(
        List<WalletDebitRequest> requests,
        CancellationToken ct
    );
    public Task CreditBackAsync(List<WalletDebitRequest> requests, CancellationToken ct);
    public Task<int> GetAmountForCurrencyAsync(CurrencyKind kind, CancellationToken ct);
    public Task<Dictionary<int, int>> GetActivityPointsAsync(CancellationToken ct);
    public Task GrantCreditsAsync(int amount, CancellationToken ct);
    public Task GrantActivityPointsAsync(int activityPointType, int amount, CancellationToken ct);
}
