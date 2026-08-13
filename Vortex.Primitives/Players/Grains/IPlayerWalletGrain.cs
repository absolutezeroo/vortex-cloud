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

    /// <summary>
    /// Credits any currency, including the ones with no dedicated grant of their own.
    /// </summary>
    /// <remarks>
    /// Silver and emeralds had no way in at all before this: they could be read and debited — the
    /// catalogue prices offers in silver — but nothing anywhere could increase them, so both sat at
    /// zero for the lifetime of an account and a silver-priced offer was unbuyable by construction.
    /// The grain has always been able to do this; only the two typed wrappers were on the interface.
    /// </remarks>
    public Task GrantCurrencyAsync(CurrencyKind kind, int amount, CancellationToken ct);
}
