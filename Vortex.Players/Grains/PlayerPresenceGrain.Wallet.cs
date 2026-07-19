using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Players.Snapshots;

namespace Vortex.Players.Grains;

internal sealed partial class PlayerPresenceGrain
{
    public Task OnCurrencyUpdateAsync(
        WalletCurrencyUpdateSnapshot snapshot,
        CancellationToken ct
    ) => _walletModule.OnCurrencyUpdateAsync(snapshot, ct);
}
